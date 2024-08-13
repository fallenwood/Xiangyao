namespace Xiangyao.Docker;

using System.Net.Http;
using System.Text.Json;


public record ContainerEventsParameters {

}

public interface IAsyncProgress<in T> {
  public ValueTask ReportAsync(T value);
}

public interface IDockerClient {
  public ValueTask<ListContainerResponse[]> ListContainersAsync();
  public Task MonitorEventsAsync(
    ContainerEventsParameters parameters,
    IAsyncProgress<MonitorEvent> progress,
    CancellationToken cancellationToken);
}

public class DockerClient(HttpClient httpClient) : IDockerClient {

  public async ValueTask<ListContainerResponse[]> ListContainersAsync() {
    var url = new Uri("/containers/list");
    var response = await httpClient.GetAsync(url);
    var stream = await response.Content.ReadAsStreamAsync();

    if (!response.IsSuccessStatusCode) {
      var error = await JsonSerializer.DeserializeAsync(stream, DockerJsonContext.Default.ErrorResponse);
      throw new Exception();
    }

    var payload = await JsonSerializer.DeserializeAsync(stream, DockerJsonContext.Default.ListContainerResponseArray);
    return payload!;
  }

  public async Task MonitorEventsAsync(
    ContainerEventsParameters parameters,
    IAsyncProgress<MonitorEvent> progress,
    CancellationToken cancellationToken) {
    var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost:2375/events");
    var response = await httpClient.SendAsync(requestMessage, completionOption: HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    var reader = new StreamReader(stream);

    while (!reader.EndOfStream) {
      var line = await reader.ReadLineAsync(cancellationToken);
      if (string.IsNullOrEmpty(line)) {
        continue;
      }

      try {
        var @event = JsonSerializer.Deserialize(line, DockerJsonContext.Default.MonitorEvent);

        await progress.ReportAsync(@event!);
      }
      catch {
        
      }
    }
  }
}
