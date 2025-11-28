# Simple MCP Server with Aspire

This is a VERY simple MCP server using the Streamable HTTP transport with Aspire.

The MCP server is based pretty closely on [this sample from the C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/AspNetCoreMcpServer).

It shows Aspire orchestration with Open Telemetry on the Aspire dashboard, and
includes the [MCP Inspector integration from the Community Toolkit](https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.McpInspector),
which has [a good readme / repo](https://github.com/CommunityToolkit/Aspire/tree/main/src/CommunityToolkit.Aspire.Hosting.McpInspector).

## Successful https connection

I was finally able to get the https connection to work by using some code from
[the Aspire JavaScript hosting extensions where it adds trust for ASP.NET dev cert
to the Node app](https://github.com/dotnet/aspire/blob/f1b9ef25cafc6e0307e691a18d154db189b1dd48/src/Aspire.Hosting.JavaScript/JavaScriptHostingExtensions.cs#L218) - which should be an extension method
call appended to the `AddMcpInspector()` method in the AppHost:

```csharp
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
```

## Original notes about being unsuccessful with https or http

Not sure why, but for some reason the MCP inspector won't connect to the MCP server without this
line in `apphost.cs` under the `AddMcpInspector()` line:

```csharp
.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0")
```

This MAY be resolved with .NET 10.0.1 which would probably
require creating new local .NET certs: [https://github.com/dotnet/aspnetcore/issues/64261](https://github.com/dotnet/aspnetcore/issues/64261)

What follows is a review of different approaches for setup with Aspire 13 and the results I saw.

### Using an http profile

Use the `launchSettings.json-http` as the real `launchSettings.json` file.  That would produce this error:

```txt
Failed to create resource mcp-inspector System.InvalidOperationException: The endpoint `https` is not allocated for the resource `mcp`. at Aspire.Hosting.ApplicationModel.EndpointReference.get_AllocatedEndpoint() in /_/src/Aspire.Hosting/ApplicationModel/EndpointReference.cs:line 134 at Aspire.Hosting.ApplicationModel.EndpointReference.get_Url() in /_/src/Aspire.Hosting/ApplicationModel/EndpointReference.cs:line 128 at Aspire.Hosting.McpInspectorResourceBuilderExtensions.<>c.<AddMcpInspector>b__1_7(McpServerMetadata s) in /_/src/CommunityToolkit.Aspire.Hosting.McpInspector/McpInspectorResourceBuilderExtensions.cs:line 82
```

### Using an https profile

Use the built-in `launchSettings.json` which has the https profile.  This will result in the error below unless you have this line in `apphost.cs` under the `AddMcpInspector` line:

```csharp
.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0")
```

The suggested fix below - using the `--use-system-ca` didn't work when I tried it.

```txt
🚀 MCP Inspector is up and running at:

[http://localhost:6274/?MCP_PROXY_AUTH_TOKEN=%7EuG%7BwKeA-%21DxJ42A%7E8mqM%29](http://localhost:6274/?MCP_PROXY_AUTH_TOKEN=%7EuG%7BwKeA-%21DxJ42A%7E8mqM%29)

New StreamableHttp connection request

Query parameters: {"url":"[https://localhost:7229/](https://localhost:7229/)","transportType":"streamable-http"}
Created StreamableHttp client transport
Client <-> Proxy sessionId: 068ebc44-8d6c-46d2-aa19-af3c0d9ac433

Error from MCP server: FetchError: request to [https://localhost:7229/](https://localhost:7229/) failed, reason: self-signed certificate; if the root CA is installed locally, try running Node.js with --use-system-ca
at ClientRequest.<anonymous> (file:///E:/demos/mcp-sample/node_modules/node-fetch/src/index.js:108:11)
at ClientRequest.emit (node:events:520:35)
at emitErrorEvent (node:_http_client:108:11)
at TLSSocket.socketErrorListener (node:_http_client:575:5)
at TLSSocket.emit (node:events:508:28)
Starting process... {"Executable": "/mcp-bekavwzd", "Reconciliation": 6, "Cmd": "C:\\Program Files\\dotnet\\dotnet.exe", "Args": ["run", "--project", "E:\\demos\\mcp-sample\\HelloMcp\\HelloMcp.csproj", "--no-build", "--configuration", "Debug", "--no-launch-profile"]}
at emitErrorNT (node:internal/streams/destroy:170:8)
at emitErrorCloseNT (node:internal/streams/destroy:129:3)
at process.processTicksAndRejections (node:internal/process/task_queues:89:21) {
type: 'system',
errno: 'DEPTH_ZERO_SELF_SIGNED_CERT',
code: 'DEPTH_ZERO_SELF_SIGNED_CERT',
erroredSysCall: undefined
}
```
