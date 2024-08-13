namespace Xiangyao.Docker;

public record RegistryAuth {
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string ServerAddress { get; set; } = string.Empty;
  public string IdentityToken { get; set; } = string.Empty;
}
