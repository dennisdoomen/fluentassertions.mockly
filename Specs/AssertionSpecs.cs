using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Sdk;

namespace Mockly.Specs;

public class AssertionSpecs
{
    public class HttpMockSpecs
    {
        [Fact]
        public async Task Can_assert_all_mocks_have_been_invoked()
        {
            // Arrange
            var mock = new HttpMock();

            mock.ForGet().WithPath("/api/test1").RespondsWithStatus(HttpStatusCode.OK);
            mock.ForGet().WithPath("/api/test2").RespondsWithStatus(HttpStatusCode.OK);

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test1");
            await client.GetAsync("https://localhost/api/test2");

            // Assert
            mock.Should().HaveAllRequestsCalled();
        }

        [Fact]
        public async Task Will_throw_when_not_all_mocks_have_been_invoked()
        {
            // Arrange
            var mock = new HttpMock();

            mock.ForGet().WithPath("/api/test1").RespondsWithStatus(HttpStatusCode.OK);
            mock.ForGet().WithPath("/api/test2").RespondsWithStatus(HttpStatusCode.OK);

            // Build step removed;
            var client = mock.GetClient();

            await client.GetAsync("https://localhost/api/test1");

            // Act
            var act = () => mock.Should().HaveAllRequestsCalled();

            // Assert
            act.Should().Throw<XunitException>().WithMessage("*but the following mock was not invoked*");
        }

        [Fact]
        public async Task Failure_message_lists_which_mocks_were_not_called()
        {
            // Arrange
            var mock = new HttpMock();

            mock.ForGet().WithPath("/api/users").RespondsWithStatus(HttpStatusCode.OK);
            mock.ForPost().WithPath("/api/orders").RespondsWithStatus(HttpStatusCode.Created);

            var client = mock.GetClient();
            await client.GetAsync("https://localhost/api/users");

            // Act
            var act = () => mock.Should().HaveAllRequestsCalled();

            // Assert - The failure message lists each uninvoked mock by HTTP method and path.
            string message = act.Should().Throw<XunitException>().Which.Message;
            message.Should().Contain("the following mock was not invoked");
            message.Should().Contain("POST");
            message.Should().Contain("/api/orders");
        }
    }

    public class RequestCollectionSpecs
    {
        [Fact]
        public async Task Can_assert_request_collection_is_not_empty()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            mock.Requests.Should().NotBeEmpty();
        }

        [Fact]
        public void Will_throw_when_request_collection_is_empty()
        {
            // Arrange
            var mock = new HttpMock();

            // Act
            var act = () => mock.Requests.Should().NotBeEmpty();

            // Assert
            act.Should().Throw<XunitException>().WithMessage("*empty*");
        }

        [Fact]
        public async Task Can_assert_no_unexpected_calls()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            mock.Requests.Should().NotContainUnexpectedCalls();
        }

        [Fact]
        public async Task Will_throw_when_unexpected_calls_are_present()
        {
            // Arrange
            var mock = new HttpMock
            {
                FailOnUnexpectedCalls = false
            };

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/unexpected");
            var act = () => mock.Requests.Should().NotContainUnexpectedCalls();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("no unexpected requests should exist, but found 1 unexpected request(s):*");
        }
    }

    public class CapturedRequestAssertions
    {
        [Fact]
        public async Task Can_assert_captured_request_is_expected()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            var request = mock.Requests.First();
            request.Should().BeExpected();
        }

        [Fact]
        public async Task Will_throw_when_captured_request_is_not_expected()
        {
            // Arrange
            var mock = new HttpMock
            {
                FailOnUnexpectedCalls = false
            };

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/unexpected");
            var request = mock.Requests.First();
            var act = () => request.Should().BeExpected();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("request should be expected, but it was unexpected");
        }

        [Fact]
        public async Task Can_assert_captured_request_is_unexpected()
        {
            // Arrange
            var mock = new HttpMock();
            mock.FailOnUnexpectedCalls = false;

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/unexpected");

            // Assert
            var request = mock.Requests.First();
            request.Should().BeUnexpected();
        }

        [Fact]
        public async Task Will_throw_when_captured_request_is_expected_but_asserted_unexpected()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Build step removed;
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");
            var request = mock.Requests.First();
            var act = () => request.Should().BeUnexpected();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("request should be unexpected, but it was expected");
        }
    }

    public class ContainRequest
    {
        [Fact]
        public async Task Can_ensure_a_request_was_captured()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            mock.Requests.Should().ContainRequest();
        }

        [Fact]
        public void Fails_when_no_requests_are_captured()
        {
            // Arrange
            var mock = new HttpMock();

            // Act
            var act = () => mock.Requests.Should().ContainRequest();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("Expected at least one request to have been captured, but none were found*");
        }
    }

    public class ContainRequestFor
    {
        [Fact]
        public async Task Finds_request_for_relative_path()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            mock.Requests.Should().ContainRequestFor("/api/t*t");
        }

        [Fact]
        public async Task Finds_request_for_absolute_uri()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            mock.Requests.Should().ContainRequestFor(new Uri("https://localhost/*/test"));
        }

        [Fact]
        public void Fails_when_no_requests_are_captured_at_all()
        {
            // Arrange
            var mock = new HttpMock();

            // Act
            var act = () => mock.Requests.Should().ContainRequestFor("/missing");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected*/missing*at all*");
        }

        [Fact]
        public async Task Fails_when_the_expected_request_is_missing()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/other").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/other");

            var act = () => mock.Requests.Should().ContainRequestFor("/api/missing");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("Expected*/api/missing**among:*GET https://localhost/api/other*");
        }
    }

    public class WithBody
    {
        [Fact]
        public async Task Matches_body_with_wildcard_pattern()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("hello world"));

            // Assert
            mock.Requests.Should().ContainRequest()
                .WithBody("*world");
        }

        [Fact]
        public async Task Can_match_multiple_requests()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("hello world"));
            await client.PostAsync("https://localhost/api/test", new StringContent("hallo wereld"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBody("*wereld*");
            mock.Requests.Should().ContainRequest().WithBody("*world*");
        }

        [Fact]
        public async Task Fails_when_body_does_not_match_wildcard_pattern()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("hello world"));

            var act = () => mock.Requests.Should().ContainRequest()
                .WithBody("abc?");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*expected request body to match wildcard pattern*");
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_body_matches_wildcard()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("foo"));
            await client.PostAsync("https://localhost/api/test", new StringContent("bar"));

            var act = () => mock.Requests.Should().ContainRequest().WithBody("baz*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request having a body that matches wildcard pattern*");
        }
    }

    public class WithBodyMatchingJson
    {
        [Fact]
        public async Task Matches_body_equivalent_json()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{\n  \"id\":1, \"name\":\"x\"\n}"));

            // Assert
            mock.Requests.Should().ContainRequest()
                .WithBodyMatchingJson("{ \"id\": 1, \"name\": \"x\" }");
        }

        [Fact]
        public async Task Can_match_against_multiple_requests()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{\n  \"id\":1, \"name\":\"x\"\n}"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{\n  \"id\":2, \"name\":\"y\"\n}"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyMatchingJson("{ \"id\": 2, \"name\": \"y\" }");
            mock.Requests.Should().ContainRequest().WithBodyMatchingJson("{ \"id\": 1, \"name\": \"x\" }");
        }

        [Fact]
        public async Task Fails_when_body_json_does_not_match()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":2 }"));
            var act = () => mock.Requests.Should().ContainRequest()
                .WithBodyMatchingJson("{ \"id\": 1 }");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*to be JSON-equivalent*");
        }

        [Fact]
        public async Task Fails_when_request_body_is_not_valid_json()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("not-json"));
            var act = () => mock.Requests.Should().ContainRequest()
                .WithBodyMatchingJson("{ \"id\": 1 }");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*request body is not valid JSON*");
        }

        [Fact]
        public async Task Fails_when_json_argument_is_empty()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\": 1 }"));
            var act = () => mock.Requests.Should().ContainRequest()
                .WithBodyMatchingJson(string.Empty);

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Cannot compare the JSON body with <null>*");
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_body_matches_json()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\": 1 }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\": 2 }"));

            var act = () => mock.Requests.Should().ContainRequest()
                .WithBodyMatchingJson("{ \"id\": 3 }");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request body to be JSON-equivalent*");
        }
    }

    public class WithBodyEquivalentTo
    {
        [Fact]
        public async Task Matches_body_equivalent_to_anonymous_object()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":2, \"name\":\"y\" }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":1, \"name\":\"x\" }"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyEquivalentTo(new
            {
                id = 1,
                name = "x"
            });

            mock.Requests.Should().ContainRequest().WithBodyEquivalentTo(new
            {
                id = 2,
                name = "y"
            });
        }

        [Fact]
        public async Task Fails_when_body_is_not_equivalent_to_anonymous_object()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":1, \"name\":\"x\" }"));

            var expected = new
            {
                id = 2,
                name = "y"
            };

            var act = () => mock.Requests.Should().ContainRequest()
                .WithBodyEquivalentTo(expected);

            // Assert
            act.Should().Throw<XunitException>().WithMessage(
                """
                Expected request #1 (POST https://localhost/api/test) to have a body equivalent to the expectation, but it did not:
                - Expected property actual.id to be 2, but found 1.
                - Expected property actual.name to be "y", but "x" differs near "x" (index 0).*
                """);
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_is_body_equivalent_to()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":1, \"name\":\"x\" }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":2, \"name\":\"y\" }"));

            var expected = new { id = 3, name = "z" };

            var act = () => mock.Requests.Should().ContainRequest().WithBodyEquivalentTo(expected);

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected the closest matching request*at have a body equivalent to the expectation*");
        }
    }

    public class WithBodyHavingPropertiesOf
    {
        [Fact]
        public async Task Matches_body_having_properties_of_dictionary()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"2\", \"name\":\"y\" }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"1\", \"name\":\"x\" }"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesOf(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "1",
                    ["name"] = "x"
                });

            mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesOf(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "2",
                    ["name"] = "y"
                });
        }

        [Fact]
        public async Task Matches_body_having_properties_of_dictionary_when_request_has_properties_with_null_values()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"2\", \"name\":null }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"1\", \"name\":\"x\" }"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesOf(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "1",
                    ["name"] = "x"
                });

            mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesOf(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "2",
                    ["name"] = null
                });
        }

        [Fact]
        public async Task Fails_when_body_does_not_have_expected_properties()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"1\" }"));

            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["id"] = "2"
            };

            var act = () => mock.Requests.Should().ContainRequest()
                .WithBodyHavingPropertiesOf(expected);

            // Assert
            act.Should().Throw<XunitException>();
        }

        [Fact]
        public async Task Fails_when_none_of_the_requests_have_the_expected_property_and_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":1 }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":2 }"));

            var act = () => mock.Requests.Should().ContainRequest().WithBodyHavingProperty("id", "3");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("Expected at least one request body to contain property \"id\" with value \"3\", but none did");
        }

        [Fact]
        public async Task Fails_when_body_does_not_have_the_expected_property_and_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":1 }"));

            var act = () => mock.Requests.Should().ContainRequest().WithBodyHavingProperty("id", "3");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("Expected the request body to contain property \"id\" with value \"3\", but it did not:*");
        }

        [Fact]
        public async Task Matches_body_having_property_when_request_has_properties_with_null_values()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":3, \"name\":null}"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyHavingProperty("id", "3");
        }

        [Fact]
        public async Task Fails_when_body_is_not_json_object()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("not-json"));

            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["id"] = "1"
            };

            var act = () => mock.Requests.Should().ContainRequest()
                .WithBodyHavingPropertiesOf(expected);

            // Assert
            act.Should().Throw<JsonException>()
                .WithMessage("*'not-json' is an invalid JSON literal*");
        }

        [Fact]
        public async Task Can_handle_different_json_types()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost("https://localhost/api/test").RespondsWithStatus(HttpStatusCode.Accepted);

            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent(
                """
                {
                  "fnv_name" : "Parent company pension plan working scope",
                  "fnv_collectivescheme@odata.bind" : "/fnv_collectiveschemes(3588777a-b78e-4716-95f7-99952c49b4cb)",
                  "fnv_grouping" : 118680000,
                  "fnv_businesssubgroup@odata.bind" : "/fnv_businesssubgroups(9f76a4a0-72d8-48a7-aaff-ca11da572130)",
                  "fnv_businessgrouping@odata.bind" : "/accounts(4d1ae18c-5568-4cc4-be94-a83275dc8992)",
                  "fnv_inheritedof@odata.bind" : "/fnv_workingscopes(af0e9547-3902-470e-9bdb-b858da7eef38)",
                  "fnv_origin" : 118680001,
                  "fnv_inherit" : false
                }
                """));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesOf(
                new Dictionary<string, string>
                {
                    { "fnv_name", "Parent company pension plan working scope" },
                    { "fnv_collectivescheme@odata.bind", "/fnv_collectiveschemes(3588777a-b78e-4716-95f7-99952c49b4cb)" },
                    { "fnv_grouping", "118680000" },
                    { "fnv_businessgrouping@odata.bind", "/accounts(4d1ae18c-5568-4cc4-be94-a83275dc8992)" },
                    { "fnv_businesssubgroup@odata.bind", "/fnv_businesssubgroups(9f76a4a0-72d8-48a7-aaff-ca11da572130)" },
                    { "fnv_inheritedof@odata.bind", "/fnv_workingscopes(af0e9547-3902-470e-9bdb-b858da7eef38)" },
                    { "fnv_origin", "118680001" },
                    { "fnv_inherit", "False" }
                });
        }

        [Fact]
        public async Task Ignores_extra_properties_in_body()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test",
                new StringContent("{ \"id\":\"1\", \"name\":\"x\", \"extra\":\"value\" }"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesOf(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "1",
                    ["name"] = "x"
                });
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_has_expected_properties()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"1\" }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"2\" }"));

            var expected = new Dictionary<string, string>(StringComparer.Ordinal) { ["id"] = "3" };

            var act = () => mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesOf(expected);

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request body to have the expected properties*but none did*");
        }
    }

    public class WithBodyHavingPropertiesEqualTo
    {
        [Fact]
        public async Task Matches_body_having_properties_equal_to_dictionary()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"1\", \"name\":\"x\" }"));

            // Assert
            mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesEqualTo(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "1",
                    ["name"] = "x"
                });
        }

        [Fact]
        public async Task Fails_when_body_has_extra_properties()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test",
                new StringContent("{ \"id\":\"1\", \"name\":\"x\", \"extra\":\"value\" }"));

            var act = () => mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesEqualTo(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "1",
                    ["name"] = "x"
                });

            // Assert
            act.Should().Throw<XunitException>();
        }

        [Fact]
        public async Task Fails_when_body_has_mismatched_property_values()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"1\" }"));

            var act = () => mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesEqualTo(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["id"] = "2"
                });

            // Assert
            act.Should().Throw<XunitException>();
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_has_equivalent_properties()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"1\" }"));
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\":\"2\" }"));

            var act = () => mock.Requests.Should().ContainRequest().WithBodyHavingPropertiesEqualTo(
                new Dictionary<string, string>(StringComparer.Ordinal) { ["id"] = "3" });

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request body to have the expected properties*but none did*");
        }
    }

    public class NotContainRequestFor
    {
        [Fact]
        public async Task Succeeds_when_no_matching_request_exists()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/other").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/other");

            // Assert + chaining via And
            mock.Requests.Should()
                .NotContainRequestFor("/api/missing")
                .And
                .NotContainUnexpectedCalls();
        }

        [Fact]
        public async Task Fails_when_a_matching_request_exists_with_clear_diagnostics()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            var act = () => mock.Requests.Should().NotContainRequestFor("/api/t*");

            act.Should().Throw<XunitException>()
                .WithMessage("Did not expect a request for URL pattern \"/api/t*\"*, but found:*GET https://localhost/api/test*");
        }
    }

    public class WithHeader
    {
        [Fact]
        public async Task Matches_when_header_is_present()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Add("X-Api-Key", "secret");
            await client.SendAsync(request);

            // Assert
            mock.Requests.Should().ContainRequest().WithHeader("X-Api-Key");
        }

        [Fact]
        public async Task Matches_header_with_wildcard_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Add("X-Api-Key", "secret-value-123");
            await client.SendAsync(request);

            // Assert
            mock.Requests.Should().ContainRequest().WithHeader("X-Api-Key", valuePattern: "secret-*");
        }

        [Fact]
        public async Task Matches_against_multiple_requests()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var req1 = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            req1.Headers.Add("X-Tenant", "tenant-a");
            await client.SendAsync(req1);

            var req2 = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            req2.Headers.Add("X-Tenant", "tenant-b");
            await client.SendAsync(req2);

            // Assert
            mock.Requests.Should().ContainRequest().WithHeader("X-Tenant", valuePattern: "tenant-a");
            mock.Requests.Should().ContainRequest().WithHeader("X-Tenant", valuePattern: "tenant-b");
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_has_header_value_matching_wildcard()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var req1 = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            req1.Headers.Add("X-Api-Key", "old-key");
            await client.SendAsync(req1);

            var req2 = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            req2.Headers.Add("X-Api-Key", "other-key");
            await client.SendAsync(req2);

            var act = () => mock.Requests.Should().ContainRequest().WithHeader("X-Api-Key", valuePattern: "new-*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request to have header*X-Api-Key*matching wildcard pattern*new-**");
        }

        [Fact]
        public async Task Fails_when_header_is_missing()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            var act = () => mock.Requests.Should().ContainRequest().WithHeader("X-Api-Key");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have header*X-Api-Key*");
        }

        [Fact]
        public async Task Fails_when_header_value_does_not_match_wildcard()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Add("X-Api-Key", "wrong-value");
            await client.SendAsync(request);

            var act = () => mock.Requests.Should().ContainRequest().WithHeader("X-Api-Key", valuePattern: "expected-*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request header*X-Api-Key*to match wildcard pattern*expected-**");
        }

        [Fact]
        public async Task Fails_with_header_absent_message_when_none_of_multiple_requests_has_header()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");
            await client.GetAsync("https://localhost/api/test");

            var act = () => mock.Requests.Should().ContainRequest().WithHeader("X-Api-Key");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request to have header*X-Api-Key*");
        }
    }

    public class WithBearerToken
    {
        [Fact]
        public async Task Matches_when_bearer_token_is_present()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mytoken");
            await client.SendAsync(request);

            // Assert
            mock.Requests.Should().ContainRequest().WithBearerToken();
        }

        [Fact]
        public async Task Matches_bearer_token_with_wildcard_pattern()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJtoken123");
            await client.SendAsync(request);

            // Assert
            mock.Requests.Should().ContainRequest().WithBearerToken(tokenPattern: "eyJ*");
        }

        [Fact]
        public async Task Matches_bearer_token_case_insensitively()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.TryAddWithoutValidation("Authorization", "bearer mytoken");
            await client.SendAsync(request);

            // Assert
            mock.Requests.Should().ContainRequest().WithBearerToken();
        }

        [Fact]
        public async Task Matches_against_multiple_requests_for_bearer_presence()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act — first request has no token, second has a bearer token
            await client.GetAsync("https://localhost/api/test");

            var req = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token");
            await client.SendAsync(req);

            // Assert
            mock.Requests.Should().ContainRequest().WithBearerToken();
        }

        [Fact]
        public async Task Fails_when_bearer_token_is_absent()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            var act = () => mock.Requests.Should().ContainRequest().WithBearerToken();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a Bearer token*no Authorization header*");
        }

        [Fact]
        public async Task Fails_when_authorization_scheme_is_not_bearer()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz");
            await client.SendAsync(request);

            var act = () => mock.Requests.Should().ContainRequest().WithBearerToken();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a Bearer token*Authorization: Basic*");
        }

        [Fact]
        public async Task Fails_when_bearer_token_value_does_not_match_wildcard()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "wrongtoken");
            await client.SendAsync(request);

            var act = () => mock.Requests.Should().ContainRequest().WithBearerToken(tokenPattern: "eyJ*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a Bearer token matching*eyJ**");
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_has_bearer_token_matching_wildcard()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var req1 = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            req1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token-one");
            await client.SendAsync(req1);

            var req2 = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            req2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token-two");
            await client.SendAsync(req2);

            var act = () => mock.Requests.Should().ContainRequest().WithBearerToken(tokenPattern: "eyJ*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request to have a Bearer token matching*eyJ**");
        }

        [Fact]
        public async Task Fails_when_bearer_token_pattern_does_not_match_and_scheme_is_not_bearer()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz");
            await client.SendAsync(request);

            var act = () => mock.Requests.Should().ContainRequest().WithBearerToken(tokenPattern: "eyJ*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a Bearer token matching*eyJ**Authorization: Basic*");
        }

        [Fact]
        public async Task Fails_when_none_of_multiple_requests_has_bearer_token()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");
            await client.GetAsync("https://localhost/api/test");

            var act = () => mock.Requests.Should().ContainRequest().WithBearerToken();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request to have a Bearer token*");
        }
    }

    public class Chaining
    {
        [Fact]
        public async Task Works_for_contained_request_assertions()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test", new StringContent("{ \"id\": \"1\" }"));

            // Assert: Contain + body assertions chained via .And
            mock.Requests.Should()
                .ContainRequestFor("/api/test")
                .WithBodyMatchingJson("{ \"id\": \"1\" }")
                .And
                .WithBodyHavingProperty("id", "1");
        }

        [Fact]
        public async Task Works_for_chaining_header_with_body_assertions()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token123");
            request.Content = new StringContent("{ \"id\": \"1\" }");
            await client.SendAsync(request);

            // Assert
            mock.Requests.Should()
                .ContainRequest()
                .WithBearerToken()
                .And
                .WithBodyHavingProperty("id", "1");
        }
    }

    public class QueryParamAssertionSpecs
    {
        [Fact]
        public async Task Finds_request_having_named_query_param_with_any_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test?name=Alice");

            // Assert
            mock.Requests.Should().ContainRequest().WithQueryParam("name");
        }

        [Fact]
        public async Task Fails_when_named_query_param_is_not_present()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test?other=value");
            var act = () => mock.Requests.Should().ContainRequest().WithQueryParam("name");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a query parameter named*name*");
        }

        [Fact]
        public async Task Finds_request_having_query_param_with_matching_wildcard_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test?name=Alice");

            // Assert
            mock.Requests.Should().ContainRequest().WithQueryParam("name", "Al*");
        }

        [Fact]
        public async Task Fails_when_query_param_value_does_not_match_wildcard()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test?name=Alice");
            var act = () => mock.Requests.Should().ContainRequest().WithQueryParam("name", "Bob*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a query parameter*name*matching wildcard*Bob**");
        }

        [Fact]
        public async Task Decodes_url_encoded_query_param_name_and_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test?full%20name=hello%20world");

            // Assert
            mock.Requests.Should().ContainRequest()
                .WithQueryParam("full name")
                .And
                .WithQueryParam("full name", "hello *");
        }

        [Fact]
        public async Task Can_match_against_multiple_requests()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test?q=foo");
            await client.GetAsync("https://localhost/api/test?q=bar");

            // Assert
            mock.Requests.Should().ContainRequest().WithQueryParam("q", "foo");
            mock.Requests.Should().ContainRequest().WithQueryParam("q", "bar");
        }

        [Fact]
        public async Task Fails_with_multi_request_message_when_none_match()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test?q=foo");
            await client.GetAsync("https://localhost/api/test?q=bar");
            var act = () => mock.Requests.Should().ContainRequest().WithQueryParam("q", "baz");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request to have a query parameter*q*matching wildcard*baz*");
        }
    }

    public class FormFieldAssertionSpecs
    {
        [Fact]
        public async Task Finds_request_having_form_field_with_matching_wildcard_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("name", "Alice"),
                    new KeyValuePair<string, string>("age", "30")
                ]));

            // Assert
            mock.Requests.Should().ContainRequest().WithFormField("name", "Al*");
        }

        [Fact]
        public async Task Fails_when_form_field_does_not_match_wildcard()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent([new KeyValuePair<string, string>("name", "Alice")]));
            var act = () => mock.Requests.Should().ContainRequest().WithFormField("name", "Bob*");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a form field*name*matching wildcard*Bob**");
        }

        [Fact]
        public async Task Fails_when_form_field_is_not_present()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent([new KeyValuePair<string, string>("name", "Alice")]));
            var act = () => mock.Requests.Should().ContainRequest().WithFormField("age", "30");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected request to have a form field*age*");
        }

        [Fact]
        public async Task Decodes_url_encoded_form_field_name_and_value()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act — FormUrlEncodedContent encodes spaces as '+', WebUtility.UrlDecode handles both
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("full name", "hello world")
                ]));

            // Assert
            mock.Requests.Should().ContainRequest().WithFormField("full name", "hello *");
        }

        [Fact]
        public async Task Can_match_against_multiple_requests()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent([new KeyValuePair<string, string>("lang", "en")]));
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent([new KeyValuePair<string, string>("lang", "nl")]));

            // Assert
            mock.Requests.Should().ContainRequest().WithFormField("lang", "en");
            mock.Requests.Should().ContainRequest().WithFormField("lang", "nl");
        }

        [Fact]
        public async Task Fails_with_multi_request_message_when_none_match()
        {
            // Arrange
            var mock = new HttpMock();
            mock.ForPost().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent([new KeyValuePair<string, string>("lang", "en")]));
            await client.PostAsync("https://localhost/api/test",
                new FormUrlEncodedContent([new KeyValuePair<string, string>("lang", "nl")]));
            var act = () => mock.Requests.Should().ContainRequest().WithFormField("lang", "de");

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected at least one request to have a form field*lang*matching wildcard*de*");
        }
    }

    public class PerMockAssertionSpecs
    {
        [Fact]
        public async Task HaveBeenCalled_passes_when_called_at_least_once()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            // Assert
            getMock.Should().HaveBeenCalled();
        }

        [Fact]
        public async Task HaveBeenCalled_passes_when_called_multiple_times()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");
            await client.GetAsync("https://localhost/api/test");

            // Assert
            getMock.Should().HaveBeenCalled();
        }

        [Fact]
        public void HaveBeenCalled_fails_when_never_called()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Act
            var act = () => getMock.Should().HaveBeenCalled();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*GET*/api/test*to have been called at least once*");
        }

        [Fact]
        public async Task HaveBeenCalled_with_exact_count_passes_when_count_matches()
        {
            // Arrange
            var mock = new HttpMock();
            var postMock = mock.ForPost().WithPath("/api/users").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/users", new StringContent("{}"));
            await client.PostAsync("https://localhost/api/users", new StringContent("{}"));
            await client.PostAsync("https://localhost/api/users", new StringContent("{}"));

            // Assert
            postMock.Should().HaveBeenCalled(3);
        }

        [Fact]
        public async Task HaveBeenCalled_with_exact_count_fails_when_count_differs()
        {
            // Arrange
            var mock = new HttpMock();
            var postMock = mock.ForPost().WithPath("/api/users").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/users", new StringContent("{}"));

            var act = () => postMock.Should().HaveBeenCalled(3);

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*POST*/api/users*exactly 3 time(s)*but it was called 1 time(s)*");
        }

        [Fact]
        public void HaveBeenCalled_with_zero_times_passes_when_not_called()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Assert — 0 times is equivalent to NotHaveBeenCalled
            getMock.Should().HaveBeenCalled(0);
        }

        [Fact]
        public void HaveBeenCalled_throws_when_negative_count_is_given()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Act
            var act = () => getMock.Should().HaveBeenCalled(-1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("times");
        }

        [Fact]
        public void NotHaveBeenCalled_passes_when_never_called()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);

            // Assert
            getMock.Should().NotHaveBeenCalled();
        }

        [Fact]
        public async Task NotHaveBeenCalled_fails_when_called()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/test").RespondsWithStatus(HttpStatusCode.OK);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/test");

            var act = () => getMock.Should().NotHaveBeenCalled();

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*GET*/api/test*to not have been called*but it was called 1 time(s)*");
        }

        [Fact]
        public async Task HaveCalledInOrder_passes_when_mocks_are_called_in_expected_order()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/step1").RespondsWithStatus(HttpStatusCode.OK);
            var postMock = mock.ForPost().WithPath("/api/step2").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act
            await client.GetAsync("https://localhost/api/step1");
            await client.PostAsync("https://localhost/api/step2", new StringContent("{}"));

            // Assert
            mock.Should().HaveCalledInOrder(getMock, postMock);
        }

        [Fact]
        public async Task HaveCalledInOrder_fails_when_mocks_are_called_in_wrong_order()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/step1").RespondsWithStatus(HttpStatusCode.OK);
            var postMock = mock.ForPost().WithPath("/api/step2").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act — POST before GET
            await client.PostAsync("https://localhost/api/step2", new StringContent("{}"));
            await client.GetAsync("https://localhost/api/step1");

            var act = () => mock.Should().HaveCalledInOrder(getMock, postMock);

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected mocks to have been called in order*");
        }

        [Fact]
        public async Task HaveCalledInOrder_fails_when_one_mock_was_never_called()
        {
            // Arrange
            var mock = new HttpMock();
            var getMock = mock.ForGet().WithPath("/api/step1").RespondsWithStatus(HttpStatusCode.OK);
            var postMock = mock.ForPost().WithPath("/api/step2").RespondsWithStatus(HttpStatusCode.Created);
            var client = mock.GetClient();

            // Act — only GET is called
            await client.GetAsync("https://localhost/api/step1");

            var act = () => mock.Should().HaveCalledInOrder(getMock, postMock);

            // Assert
            act.Should().Throw<XunitException>()
                .WithMessage("*Expected mocks to have been called in order*mock #2*never invoked*");
        }

        [Fact]
        public async Task HaveCalledInOrder_passes_with_three_mocks_in_correct_order()
        {
            // Arrange
            var mock = new HttpMock();
            var createMock = mock.ForPost().WithPath("/api/create").RespondsWithStatus(HttpStatusCode.Created);
            var readMock = mock.ForGet().WithPath("/api/read").RespondsWithStatus(HttpStatusCode.OK);
            var deleteMock = mock.ForDelete().WithPath("/api/delete").RespondsWithStatus(HttpStatusCode.NoContent);
            var client = mock.GetClient();

            // Act
            await client.PostAsync("https://localhost/api/create", new StringContent("{}"));
            await client.GetAsync("https://localhost/api/read");
            await client.DeleteAsync("https://localhost/api/delete");

            // Assert
            mock.Should().HaveCalledInOrder(createMock, readMock, deleteMock);
        }

        [Fact]
        public void HaveCalledInOrder_throws_when_no_mocks_provided()
        {
            // Arrange
            var mock = new HttpMock();

            // Act
            var act = () => mock.Should().HaveCalledInOrder();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("expectedOrder");
        }

        [Fact]
        public void HaveBeenCalled_failure_message_includes_method_and_path()
        {
            // Arrange
            var mock = new HttpMock();
            var patchMock = mock.ForPatch().WithPath("/api/resource/1").RespondsWithStatus(HttpStatusCode.OK);

            // Act
            var act = () => patchMock.Should().HaveBeenCalled();

            // Assert
            string message = act.Should().Throw<XunitException>().Which.Message;
            message.Should().Contain("PATCH");
            message.Should().Contain("/api/resource/1");
        }
    }
}
