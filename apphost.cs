#:sdk Aspire.AppHost.Sdk@13.0.0
#:package CommunityToolkit.Aspire.Hosting.McpInspector@13.0.0
#:project HelloMcp/HelloMcp.csproj

var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.HelloMcp>("mcp");

builder.AddMcpInspector("mcp-inspector")
    //.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0")
    .WithCertificateTrustConfiguration((ctx) =>
            {
                if (ctx.Scope == CertificateTrustScope.Append)
                {
                    ctx.EnvironmentVariables["NODE_EXTRA_CA_CERTS"] = ctx.CertificateBundlePath;
                }
                else
                {
                    if (ctx.EnvironmentVariables.TryGetValue("NODE_OPTIONS", out var existingOptionsObj))
                    {
                        ctx.EnvironmentVariables["NODE_OPTIONS"] = existingOptionsObj switch
                        {
                            // Attempt to append to existing NODE_OPTIONS if possible, otherwise overwrite
                            string s when !string.IsNullOrEmpty(s) => $"{s} --use-openssl-ca",
                            ReferenceExpression re => ReferenceExpression.Create($"{re} --use-openssl-ca"),
                            _ => "--use-openssl-ca",
                        };
                    }
                    else
                    {
                        ctx.EnvironmentVariables["NODE_OPTIONS"] = "--use-openssl-ca";
                    }
                }

                return Task.CompletedTask;
            })
    .WithMcpServer(mcp, path: "");

builder.Build().Run();
