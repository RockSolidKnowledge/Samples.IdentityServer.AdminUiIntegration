using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Rsk.Saml.DuendeIdentityServer.DynamicProviders;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Stores;
using Xunit;

namespace AdminUIIntegration.Tests;

public class CustomSamlIdentityProviderStoreTests
{
    private readonly Mock<IConfigurationDbContext> mockConfigurationDbContext = new Mock<IConfigurationDbContext>();
    
    [Fact]
    public void MapIdp_WhenSamlIdentityProvider_ExpectIdentityProvider()
    {
        var identityProviderEfEntity = new Duende.IdentityServer.EntityFramework.Entities.IdentityProvider
        {
            Scheme = "scheme",
            DisplayName = "name",
            Type = "saml2p",
            Enabled = true,
            Properties= 
                    "{\"ServiceProviderOptions\":" +
                    "{\"EntityId\":\"SpEntityId\"," +
                    "\"MetadataPath\":\"/saml\"," +
                    "\"MetadataOptions\":{\"CacheDuration\":\"PT1H\"}," +
                    "\"SignAuthenticationRequests\":false," +
                    "\"ValidationCertificates\":[]," +
                    "\"RequireEncryptedAssertions\":false," +
                    "\"WantAssertionsSigned\":false}," +
                "\"IdentityProviderMetadataAddress\":\"MetadataAddress\"," + 
                "\"IdentityProviderMetadataRefreshInterval\":\"12:00:00\"," +
                "\"IdentityProviderMetadataRequireHttps\":true," +
                "\"RequireValidMetadataSignature\":true," +
                "\"MessageTrustLength\":\"00:05:00\"," +
                "\"TimeComparisonTolerance\":0," +
                "\"ForceAuthentication\":false," +
                "\"ProtocolBinding\":\"urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST\"," +
                "\"NameIdClaimType\":\"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier\"," +
                "\"SignedOutCallbackPath\":\"\"," +
                "\"IdPInitiatedSsoCompletionPath\":\"\"," +
                "\"AllowIdpInitiatedSso\":false," +
                "\"SkipUnrecognizedRequests\":false," +
                "\"RequireSamlResponseDestination\":true," +
                "\"RequireSamlMessageDestination\":true," +
                "\"RequireSignedLogoutRequests\":false," +
                "\"RequireSignedLogoutResponses\":false," +
                "\"RequireAuthenticatedUserForLogoutRequests\":false," +
                "\"RequireSignedArtifactResponses\":true," +
                "\"RequireSignedArtifactResolveRequests\":true," +
                "\"AllowedIdpInitiatedRelayStates\":[]," +
                "\"ThrowOnLogoutErrors\":true," +
                "\"SamlCspOptions\":" +
                    "{\"Level\":1," +
                    "\"AddDeprecatedHeader\":true}," +
                "\"ArtifactDeliveryBindingType\":\"urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect\"," +
                "\"ArtifactLifeTime\":\"00:05:00\"," +
                "\"ArtifactResolutionService\":\"\"," +
                "\"SigningOptions\":" +
                    "{\"CanonicalizationMethod\":\"http://www.w3.org/2001/10/xml-exc-c14n#\"," +
                    "\"DigestAlgorithm\":\"http://www.w3.org/2001/04/xmlenc#sha256\"," +
                    "\"SignatureAlgorithm\":\"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256\"}," +
                "\"LogSamlMessages\":false," +
                "\"SkipAuthnContextCheck\":false," +
                "\"CallbackPath\":\"/callbackPath\"," +
                "\"SignInScheme\":\"SignInScheme\"," +
                "\"SaveTokens\":false," +
                "\"RemoteAuthenticationTimeout\":\"00:15:00\"," +
                "\"AccessDeniedPath\":\"\"," +
                "\"BackchannelTimeout\":\"00:01:00\"," +
                "\"ReturnUrlParameter\":\"ReturnUrl\"}"
        };
        var sut = CreateSut();

        var result = sut.MapIdp(identityProviderEfEntity);

        Assert.NotNull(result);
        Assert.Equal(result.Scheme, identityProviderEfEntity.Scheme);
        Assert.Equal(result.DisplayName, identityProviderEfEntity.DisplayName);
        Assert.Equal(result.Type, identityProviderEfEntity.Type);
        Assert.Equal(result.Enabled, identityProviderEfEntity.Enabled);

        var samlOptions = new SamlDynamicIdentityProvider(result).SamlAuthenticationOptions;
        
        var optionsToString = JsonConvert.SerializeObject(samlOptions, SamlDynamicIdentityProvider.SerializerSettings);
        
        Assert.Equal(optionsToString, identityProviderEfEntity.Properties);
    }
    
    private TestCustomSamlIdentityProviderStore CreateSut()
    {
        return new TestCustomSamlIdentityProviderStore(mockConfigurationDbContext.Object, new NullLogger<IdentityProviderStore>(), new Mock<ICancellationTokenProvider>().Object);
    }

    internal class TestCustomSamlIdentityProviderStore : CustomSamlIdentityProviderStore
    {
        public TestCustomSamlIdentityProviderStore(IConfigurationDbContext context, ILogger<IdentityProviderStore> logger, ICancellationTokenProvider cancellationTokenProvider) : base(context, logger, cancellationTokenProvider)
        {
        }

        public new Duende.IdentityServer.Models.IdentityProvider MapIdp(Duende.IdentityServer.EntityFramework.Entities.IdentityProvider idp) => base.MapIdp(idp);
    }
}