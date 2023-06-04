using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Services.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEventBus();
builder.Services.AddScoped<RpcClient, RpcClient>();
var provider = builder.Services.BuildServiceProvider();
var rpcClient = provider.GetService<RpcClient>();
var response = await rpcClient.CallAsync(string.Join("", args));
Console.WriteLine(" [x] Got {0}", response);