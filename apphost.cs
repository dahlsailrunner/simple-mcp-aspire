#:sdk Aspire.AppHost.Sdk@13.0.0
#:package CommunityToolkit.Aspire.Hosting.McpInspector@13.0.0
#:project HelloMcp/HelloMcp.csproj

var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.HelloMcp>("mcp");

builder.AddMcpInspector("mcp-inspector")
    .WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0")
    .WithMcpServer(mcp, path: "");

builder.Build().Run();
