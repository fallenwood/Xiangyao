namespace Xiangyao.Utils;

using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

public class DeflateCompressionProviderOptions : IOptions<DeflateCompressionProviderOptions> {
  public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Fastest;

  public DeflateCompressionProviderOptions Value => this;
}

public class DeflateCompressionProvider(IOptions<DeflateCompressionProviderOptions> options) : ICompressionProvider {
  private DeflateCompressionProviderOptions Options => options.Value;
  public string EncodingName => "deflate";

  public bool SupportsFlush => true;

  public Stream CreateStream(Stream outputStream) {
    return new DeflateStream(outputStream, compressionLevel: Options.CompressionLevel, leaveOpen: true);
  }
}
