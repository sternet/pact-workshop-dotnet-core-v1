using Consumer;
using PactNet;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;

namespace ConsumerTests;

public class ConsumerPactTests
{
    private IPactBuilder PactBuilder { get; set; }
    private IMockProviderService MockProviderService { get; set; }

    private int MockServerPort { get { return 9222; } }
    private string MockProviderServiceBaseUri { get { return String.Format("http://localhost:{0}", MockServerPort); } }

    // This class is responsible for setting up a shared mock server for Pact used by all the tests.
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Using Spec version 2.0.0 more details at https://goo.gl/UrBSRc
        var pactConfig = new PactConfig
        {
            SpecificationVersion = "2.0.0",
            PactDir = @"..\..\..\..\..\pacts",
            LogDir = @".\pact_logs"
        };

        PactBuilder = new PactBuilder(pactConfig);

        PactBuilder.ServiceConsumer("Consumer").HasPactWith("Provider");

        MockProviderService = PactBuilder.MockService(MockServerPort);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        PactBuilder.Build();
    }

    [Test]
    public void ItHandlesInvalidDateParam()
    {
        // Arrange
        var invalidRequestMessage = "validDateTime is not a date or time";
        MockProviderService.Given("There is data")
            .UponReceiving("A invalid GET request for Date Validation with invalid date parameter")
            .With(new ProviderServiceRequest 
            {
                Method = HttpVerb.Get,
                Path = "/api/provider",
                Query = "validDateTime=lolz"
            })
            .WillRespondWith(new ProviderServiceResponse {
                Status = 400,
                Headers = new Dictionary<string, object>
                {
                    { "Content-Type", "application/json; charset=utf-8" }
                },
                Body = new 
                {
                    message = invalidRequestMessage
                }
            });
        
        // Act
        var result = ConsumerApiClient.ValidateDateTimeUsingProviderApi("lolz", MockProviderServiceBaseUri).GetAwaiter().GetResult();
        var resultBodyText = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        // Assert
        StringAssert.Contains(invalidRequestMessage, resultBodyText);
    }
}