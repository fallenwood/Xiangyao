namespace Xiangyao.Docker;

using System.Net.Http;
using System.Text.Json;
using System.Linq;

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

public class DockerClient(HttpClient httpClient, string baseUrl = "http://localhost") : IDockerClient {
  public async ValueTask<ListContainerResponse[]> ListContainersAsync() {
    var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/containers/json?all=true");
    var response = await httpClient.SendAsync(requestMessage);
    var stream = await response.Content.ReadAsStreamAsync();

    if (!response.IsSuccessStatusCode) {
      var error = await JsonSerializer.DeserializeAsync(stream, DockerJsonContext.Default.ErrorResponse);
      throw new Exception();
    }

    var payload = await JsonSerializer.DeserializeAsync(stream, DockerJsonContext.Default.ListContainerDockerResponseArray);
    
    return payload?.Select(r => new ListContainerResponse {
      Id = r.Id,
      Labels = r.Labels.Select(e => new Label { Name = e.Key, Value = e.Value}).ToArray(),
      Names = r.Names,
      NetworkSettings = r.NetworkSettings.Networks.Select(e => new NetworkEntry {
        Name = e.Key,
        IPAddress = e.Value.IPAddress,
        GlobalIPv6Address = e.Value.GlobalIPv6Address,
      }).ToArray(),
    }).ToArray() ?? [];
  }

  public async Task MonitorEventsAsync(
    ContainerEventsParameters parameters,
    IAsyncProgress<MonitorEvent> progress,
    CancellationToken cancellationToken) {
    var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/events?type=container");
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
