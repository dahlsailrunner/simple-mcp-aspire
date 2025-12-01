var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.HelloMcp>("mcp");

builder.AddMcpInspector("mcp-inspector")    
    .WithMcpServer(mcp, path: "");

builder.Build().Run();
