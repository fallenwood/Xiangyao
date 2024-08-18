var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

app.MapGet("/", static () => "Hello World");

await app.RunAsync();
