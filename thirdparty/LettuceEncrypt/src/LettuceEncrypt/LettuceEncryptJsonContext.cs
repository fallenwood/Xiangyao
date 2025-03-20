namespace LettuceEncrypt;

using System.Text.Json.Serialization;
using LettuceEncrypt.Accounts;


/// <summary>
/// 
/// </summary>
[JsonSerializable(typeof(AccountModel))]
[JsonSerializable(typeof(LettuceEncryptOptions))]
public partial class LettuceEncryptJsonContext : JsonSerializerContext {

}
