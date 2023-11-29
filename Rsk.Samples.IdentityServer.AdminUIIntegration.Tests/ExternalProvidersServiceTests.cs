using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Moq;
using Rsk.AspNetCore.Authentication.Saml2p;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Services;
using Xunit;

namespace AdminUIIntegration.Tests;

public class ExternalProvidersServiceTests
{
    private readonly Mock<IAuthenticationSchemeProvider> authenticationSchemeProviderMock = new();
    private readonly Mock<IIdentityProviderStore> identityProviderStoreMock = new();
    private readonly Mock<IConfiguration> configurationMock = new();

    private readonly List<AuthenticationScheme> fakeAuthSchemesIncRsk = new List<AuthenticationScheme>
    {
        new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler)),
        new AuthenticationScheme("rsk-oidc-scheme", "RSK OIDC Scheme", typeof(OpenIdConnectHandler)),
        new AuthenticationScheme("rsk-saml-scheme", "RSK SAML Scheme", typeof(Saml2pAuthenticationHandler)),
    };
    
    private readonly List<IdentityProviderName> fakeDuendeSchemes = new List<IdentityProviderName>
    {
        new IdentityProviderName { Scheme = "disabled-scheme", DisplayName = "Disabled Scheme", Enabled = false },
        new IdentityProviderName { Scheme = "duende-oidc-scheme", DisplayName = "Duende OIDC Scheme", Enabled = true },
        new IdentityProviderName { Scheme = "duende-saml-scheme", DisplayName = "Duende SAML Scheme", Enabled = true },
    };

    public ExternalProvidersServiceTests()
    {
        authenticationSchemeProviderMock.Setup(x => x.GetAllSchemesAsync())
            .ReturnsAsync(fakeAuthSchemesIncRsk);
        
        identityProviderStoreMock.Setup(x => x.GetAllSchemeNamesAsync())
            .ReturnsAsync(fakeDuendeSchemes);
    }

    private ExternalProviderService CreateSut()
    {
        return new ExternalProviderService(authenticationSchemeProviderMock?.Object, identityProviderStoreMock?.Object, configurationMock?.Object);
    }

    private void SetConfig(string value)
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x.Value).Returns(value);
        
        configurationMock.Setup(x => x.GetSection("DynamicAuth:Mode"))
            .Returns(mockSection.Object);
    }

    [Fact] public async Task GetAll_WhenDisabled_ShouldBeEmpty()
    {
        SetConfig("Disabled"); //Could be anything as long as it isn't Rsk or Duende
        
        var sut = CreateSut();

        var actual = await sut.GetAll();

        actual.Should().BeEmpty();
    }

    [Fact] public async Task GetAll_WhenDuende_ShouldOnlyReturnDuendeEnabledDynamicProviders()
    {
        SetConfig("Duende");
        
        var sut = CreateSut();

        var actual = await sut.GetAll();

        var expected = new List<ExternalProvider>
        {
            new ExternalProvider { AuthenticationScheme = "duende-oidc-scheme", DisplayName = "Duende OIDC Scheme", },
            new ExternalProvider { AuthenticationScheme = "duende-saml-scheme", DisplayName = "Duende SAML Scheme", },
        };

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact] public async Task GetAll_WhenRsk_ShouldOnlyReturnRskDynamicProviders()
    {
        SetConfig("Rsk");
        
        var sut = CreateSut();

        var actual = await sut.GetAll();
        
        var expected = new List<ExternalProvider>
        {
            new ExternalProvider { AuthenticationScheme = "rsk-oidc-scheme", DisplayName = "RSK OIDC Scheme", },
            new ExternalProvider { AuthenticationScheme = "rsk-saml-scheme", DisplayName = "RSK SAML Scheme", },
        };

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetScheme_WhenDisabled_ShouldNotReturnAnything()
    {
        SetConfig("Disabled"); //Could be anything as long as it isn't Rsk or Duende
        
        var sut = CreateSut();

        var actualDuende = await sut.GetScheme("duende-oidc-scheme");

        actualDuende.Should().BeNull();

        var actualRsk = await sut.GetScheme("rsk-oidc-scheme");

        actualRsk.Should().BeNull();
    }

    [Fact]
    public async Task GetScheme_WhenDuende_ShouldNotReturnDuendeExternalProvider()
    {
        SetConfig("Duende");
        
        var sut = CreateSut();

        var actualDuende = await sut.GetScheme("duende-oidc-scheme");

        actualDuende.Should().NotBeNull();
        actualDuende.AuthenticationScheme.Should().Be("duende-oidc-scheme");
        actualDuende.DisplayName.Should().Be("Duende OIDC Scheme");

        var actualRsk = await sut.GetScheme("rsk-saml-scheme");

        actualRsk.Should().BeNull();
    }

    [Fact]
    public async Task GetScheme_WhenRsk_ShouldNotReturnRskExternalProvider()
    {
        SetConfig("Rsk");
        
        var sut = CreateSut();

        var actualDuende = await sut.GetScheme("duende-oidc-scheme");

        actualDuende.Should().BeNull();

        var actualRsk = await sut.GetScheme("rsk-saml-scheme");

        actualRsk.Should().NotBeNull();
        actualRsk.AuthenticationScheme.Should().Be("rsk-saml-scheme");
        actualRsk.DisplayName.Should().Be("RSK SAML Scheme");
    }
}