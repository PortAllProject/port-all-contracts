namespace AElf.Client;

public sealed class AElfClientBuilder
{
    private string NodeEndpoint { get; set; }
    private int Timeout { get; set; }

    private string? UserName { get; set; }
    private string? Password { get; set; }

    private bool IsUseCamelCase { get; set; }

    public AElfClientBuilder()
    {
        NodeEndpoint = AElfClientConstants.LocalEndpoint;
        Timeout = 60;
    }

    public AElfClientBuilder UseEndpoint(string endpoint)
    {
        NodeEndpoint = endpoint;
        return this;
    }

    public AElfClientBuilder UsePublicEndpoint(EndpointType endpointType)
    {
        switch (endpointType)
        {
            case EndpointType.MainNetMainChain:
                NodeEndpoint = AElfClientConstants.MainNetMainChain;
                break;
            case EndpointType.MainNetSideChain1:
                NodeEndpoint = AElfClientConstants.MainNetSideChain1;
                break;
            case EndpointType.TestNetMainChain:
                NodeEndpoint = AElfClientConstants.TestNetMainChain;
                break;
            case EndpointType.TestNetSideChain2:
                NodeEndpoint = AElfClientConstants.TestNetSideChain2;
                break;
            case EndpointType.Local:
            default:
                NodeEndpoint = AElfClientConstants.LocalEndpoint;
                break;
        }

        return this;
    }

    public AElfClientBuilder SetHttpTimeout(int timeout)
    {
        Timeout = timeout;
        return this;
    }

    public AElfClientBuilder ManagePeerInfo(string? userName, string? password)
    {
        UserName = userName;
        Password = password;
        return this;
    }

    public AElfClientBuilder UseCamelCase(bool isUseCamelCase)
    {
        IsUseCamelCase = isUseCamelCase;
        return this;
    }

    public AElfClient Build()
    {
        return new AElfClient(NodeEndpoint, Timeout, UserName, Password, IsUseCamelCase);
    }
}