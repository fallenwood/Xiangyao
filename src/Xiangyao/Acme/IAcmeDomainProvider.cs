namespace Xiangyao;

public interface IAcmeDomainProvider {
  string[] DomainNames { get; }
  void SetDomainNames(string[] domainNames);
}

public class AcmeDomainProvider : IAcmeDomainProvider {
  private volatile string[] domainNames = [];

  public string[] DomainNames => this.domainNames;

  public void SetDomainNames(string[] domainNames) {
    this.domainNames = domainNames;
  }
}
