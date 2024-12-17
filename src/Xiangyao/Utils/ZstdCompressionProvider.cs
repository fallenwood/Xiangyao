namespace Xiangyao.Utils;

using System.IO;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using ZstdSharp;

public class ZstdCompressionProviderOptions
  : IOptions<ZstdCompressionProviderOptions> {
  public int Level { get; set; } = 3;
  public int BufferSize { get; set; } = 0;

  public ZstdCompressionProviderOptions Value => this;
}

public class ZstdCompressionProvider(IOptions<ZstdCompressionProviderOptions> options)
  : ICompressionProvider {
  private ZstdCompressionProviderOptions Options => options.Value;

  public string EncodingName => "zstd";

  public bool SupportsFlush => true;

  public Stream CreateStream(Stream outputStream) {
    return new CompressionStream(
      outputStream,
      level: this.Options.Level,
      bufferSize: this.Options.BufferSize,
      leaveOpen: true);
  }
}
