namespace Xiangyao.Docker;

using System.Linq;
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
    IProgress<MonitorEvent> progress,
    CancellationToken cancellationToken);
}

public class DockerClient(HttpClient httpClient, string baseUrl = "http://localhost") : IDockerClient {
  public async ValueTask<ListContainerResponse[]> ListContainersAsync() {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/containers/json?all=true");
    using var response = await httpClient.SendAsync(requestMessage);
    await using var stream = await response.Content.ReadAsStreamAsync();

    if (!response.IsSuccessStatusCode) {
      var error = await JsonSerializer.DeserializeAsync(stream, DockerJsonContext.Default.ErrorResponse);
      throw new Exception();
    }

    var payload = await JsonSerializer.DeserializeAsync(stream, DockerJsonContext.Default.ListContainerDockerResponseArray);
    if (payload is null || payload.Length == 0) {
      return [];
    }

    var containers = new ListContainerResponse[payload.Length];

    for (var containerIndex = 0; containerIndex < payload.Length; containerIndex++) {
      var sourceContainer = payload[containerIndex];
      var labels = new Label[sourceContainer.Labels.Count];
      var labelIndex = 0;

      foreach (var label in sourceContainer.Labels) {
        labels[labelIndex++] = new Label {
          Name = label.Key,
          Value = label.Value,
        };
      }

      var networkSettings = new NetworkEntry[sourceContainer.NetworkSettings.Networks.Count];
      var networkIndex = 0;

      foreach (var network in sourceContainer.NetworkSettings.Networks) {
        networkSettings[networkIndex++] = new NetworkEntry {
          Name = network.Key,
          IPAddress = network.Value.IPAddress,
          GlobalIPv6Address = network.Value.GlobalIPv6Address,
        };
      }

      containers[containerIndex] = new ListContainerResponse {
        Id = sourceContainer.Id,
        Labels = labels,
        Names = sourceContainer.Names,
        NetworkSettings = networkSettings,
      };
    }

    return containers;
  }

  public async Task MonitorEventsAsync(
    ContainerEventsParameters parameters,
    IProgress<MonitorEvent> progress,
    CancellationToken cancellationToken) {
    var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/events?type=container");
    var response = await httpClient.SendAsync(requestMessage, completionOption: HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    var reader = new StreamReader(stream);

    string? line;
    while ((line = await reader.ReadLineAsync(cancellationToken)) is not null) {
      if (string.IsNullOrEmpty(line)) {
        continue;
      }

      try {
        var @event = JsonSerializer.Deserialize(line, DockerJsonContext.Default.MonitorEvent);

        progress.Report(@event!);
      }
      catch {
        
      }
    }
  }
}
