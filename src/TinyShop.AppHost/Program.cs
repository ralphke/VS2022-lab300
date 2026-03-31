var builder = DistributedApplication.CreateBuilder(args);

var products = builder.AddProject<Projects.Products>("products");

var agentGateway = builder.AddProject<Projects.AgentGateway>("agent-gateway")
    .WaitFor(products)
    .WithReference(products);

builder.AddProject<Projects.Store>("store")
    .WaitFor(products)
    .WithReference(products);

builder.Build().Run();
