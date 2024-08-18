using System.Text.Json;
using Xiangyao.Docker;

namespace Xiangyao.UnitTests;

public class ListContainerTest {
    public const string Response = $$"""
    [
    {
        "Id": "cf1da2314e68060af7bc32a8f2990843312b451763d5fe593eff18511b9d2929",
        "Names": [
          "/docker-postgres-1"
        ],
        "Image": "postgres:15.1",
        "ImageID": "sha256:02547253a07e6edd0c070caba1d2a019b7dc7df98b948dc9a909e1808eb77024",
        "Command": "docker-entrypoint.sh postgres",
        "Created": 1710858994,
        "Ports": [],
        "Labels": {
          "com.docker.compose.config-hash": "c3367a20a4c72321a864653d45ef6dab85881fbd7eca4e5207788f7594e436dd",
          "com.docker.compose.container-number": "1",
          "com.docker.compose.depends_on": "",
          "com.docker.compose.image": "sha256:02547253a07e6edd0c070caba1d2a019b7dc7df98b948dc9a909e1808eb77024",
          "com.docker.compose.oneoff": "False",
          "com.docker.compose.project": "docker",
          "com.docker.compose.project.config_files": "/app/docker/docker-compose.yaml",
          "com.docker.compose.project.working_dir": "/app/docker",
          "com.docker.compose.service": "postgres",
          "com.docker.compose.version": "2.24.7"
        },
        "State": "running",
        "Status": "Up 13 days",
        "HostConfig": {
          "NetworkMode": "host"
        },
        "NetworkSettings": {
          "Networks": {
            "host": {
              "IPAMConfig": null,
              "Links": null,
              "Aliases": null,
              "MacAddress": "",
              "DriverOpts": null,
              "NetworkID": "7db0192baaa51ddfafbf4627f6951d652cdd77f4a33a1a24f5ef7f99c764672a",
              "EndpointID": "a309314fa3c26c2e5d0d89a778e32eecf265e818e5369e5c275414d16c9a005f",
              "Gateway": "",
              "IPAddress": "",
              "IPPrefixLen": 0,
              "IPv6Gateway": "",
              "GlobalIPv6Address": "",
              "GlobalIPv6PrefixLen": 0,
              "DNSNames": null
            }
          }
        },
        "Mounts": [
          {
            "Type": "bind",
            "Source": "/app/docker/postgres/data",
            "Destination": "/var/lib/postgresql/data",
            "Mode": "rw",
            "RW": true,
            "Propagation": "rprivate"
          }
        ]
    }
    ]
    """;

    [Fact]
    public void TestParse() {
        var response = JsonSerializer.Deserialize(Response, DockerJsonContext.Default.ListContainerDockerResponseArray);

        response.Should().NotBeNull();
        response!.Length.Should().Be(1);
        response![0].NetworkSettings.Networks.Should().NotBeEmpty();
    }
}