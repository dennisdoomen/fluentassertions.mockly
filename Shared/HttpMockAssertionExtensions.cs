using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Mockly.Common;

#pragma warning disable CA1054

#pragma warning disable AV1505
namespace Mockly;
#pragma warning restore AV1505

// This file intentionally contains multiple assertion types for discoverability and
// to keep the FluentAssertions extensions grouped together. The MA0048 rule enforces
// a file name to match a single type name, which doesn't apply to this design.
#pragma warning disable MA0048 // File name must match type name

/// <summary>
/// FluentAssertions extensions for HttpMock.
/// </summary>
public static class HttpMockAssertionExtensions
{
    /// <summary>
    /// Returns an assertion object for the HttpMock.
    /// </summary>
    public static HttpMockAssertions Should(this HttpMock mock)
    {
        return new HttpMockAssertions(mock);
    }

    /// <summary>
    /// Returns an assertion object for the RequestCollection.
    /// </summary>
    public static RequestCollectionAssertions Should(this RequestCollection collection)
    {
        return new RequestCollectionAssertions(collection);
    }

    /// <summary>
    /// Returns an assertion object for the CapturedRequest.
    /// </summary>
    public static CapturedRequestAssertions Should(this CapturedRequest request)
    {
        return new CapturedRequestAssertions(request);
    }

    /// <summary>
    /// Returns an assertion object for the RequestMockResponseBuilder, enabling per-mock invocation assertions.
    /// </summary>
    public static RequestMockResponseBuilderAssertions Should(this RequestMockResponseBuilder builder)
    {
        return new RequestMockResponseBuilderAssertions(builder);
    }
}

/// <summary>
/// Assertions for HttpMock.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name",
    Justification = "Multiple assertion classes in one file for convenience")]
public class HttpMockAssertions(HttpMock subject) :
#if FA8
    ReferenceTypeAssertions<HttpMock, HttpMockAssertions>(subject, AssertionChain.GetOrCreate())
#else
    ReferenceTypeAssertions<HttpMock, HttpMockAssertions>(subject)
#endif
{
    private readonly HttpMock subject = subject;

    /// <summary>
    /// Asserts that all configured request mocks have been invoked.
    /// </summary>
    public AndConstraint<HttpMockAssertions> HaveAllRequestsCalled(string because = "", params object[] becauseArgs)
    {
        List<RequestMock> uninvoked = subject.GetUninvokedMocks().ToList();

        var failureMessage = uninvoked.Count == 1
            ? new StringBuilder("all request mocks should have been called, but the following mock was not invoked:")
            : new StringBuilder("all request mocks should have been called, but the following ")
                .Append(uninvoked.Count)
                .Append(" mocks were not invoked:");

        foreach (RequestMock mock in uninvoked)
        {
            failureMessage.Append("\n  - ").Append(mock.Method).Append(' ').Append(
                string.IsNullOrEmpty(mock.PathPattern) ? "(any path)" : mock.PathPattern);
        }

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(subject.AllMocksInvoked)
            .FailWith(failureMessage.ToString());

        return new AndConstraint<HttpMockAssertions>(this);
    }

    /// <summary>
    /// Asserts that the mocks were invoked in the specified order, based on the first observed invocation of each mock.
    /// </summary>
    /// <param name="expectedOrder">
    /// The mocks in the order they are expected to have been invoked. The assertion verifies that the first observed
    /// request for each successive mock occurred after the first observed request for the preceding mock.
    /// </param>
    /// <remarks>
    /// Order is determined by <see cref="CapturedRequest.Sequence"/>, which reflects capture order.
    /// When requests are made concurrently, the captured order may be nondeterministic.
    /// </remarks>
    public AndConstraint<HttpMockAssertions> HaveCalledInOrder(params RequestMockResponseBuilder[] expectedOrder)
    {
        if (expectedOrder is null || expectedOrder.Length == 0)
        {
            throw new ArgumentException("At least one mock must be provided to assert call order.", nameof(expectedOrder));
        }

        for (int i = 0; i < expectedOrder.Length; i++)
        {
            if (expectedOrder[i] is null)
            {
                throw new ArgumentException($"The expected order contains a null element at index {i}.", nameof(expectedOrder));
            }
        }

        var capturedRequests = subject.Requests.ToList();

        var firstOccurrences = expectedOrder
            .Select(b => (mock: b.RequestMock, request: capturedRequests.FirstOrDefault(r => ReferenceEquals(r.Mock, b.RequestMock))))
            .ToArray();

        bool succeeded = true;
        var failureMessage = new StringBuilder();

        for (int i = 0; i < firstOccurrences.Length; i++)
        {
            if (firstOccurrences[i].request is null)
            {
                succeeded = false;
                failureMessage.Append("Expected mocks to have been called in order, but mock #")
                    .Append(i + 1)
                    .Append(" (")
                    .Append(DescribeMock(firstOccurrences[i].mock))
                    .Append(") was never invoked.");
                break;
            }
        }

        if (succeeded)
        {
            for (int i = 1; i < firstOccurrences.Length; i++)
            {
                if (firstOccurrences[i].request!.Sequence <= firstOccurrences[i - 1].request!.Sequence)
                {
                    succeeded = false;
                    failureMessage.Append("Expected mocks to have been called in order, but ")
                        .Append(DescribeMock(firstOccurrences[i].mock))
                        .Append(" (first called at request #")
                        .Append(firstOccurrences[i].request!.Sequence)
                        .Append(") was not called after ")
                        .Append(DescribeMock(firstOccurrences[i - 1].mock))
                        .Append(" (first called at request #")
                        .Append(firstOccurrences[i - 1].request!.Sequence)
                        .Append(").");
                    break;
                }
            }
        }

        if (!succeeded)
        {
            failureMessage.Append(Environment.NewLine)
                .Append("Actual captured requests:")
                .Append(Environment.NewLine)
                .Append(DescribeCapturedRequests(capturedRequests));
        }

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .ForCondition(succeeded)
            .FailWith(failureMessage.ToString());

        return new AndConstraint<HttpMockAssertions>(this);
    }

    private static string DescribeMock(RequestMock mock)
        => $"{mock.Method} {(string.IsNullOrEmpty(mock.PathPattern) ? "(any path)" : mock.PathPattern)}";

    private static string DescribeCapturedRequests(IEnumerable<CapturedRequest> requests)
    {
        var builder = new StringBuilder();
        foreach (CapturedRequest r in requests)
        {
            builder.Append("  #").Append(r.Sequence).Append(": ").AppendLine(r.ToString());
        }

        return builder.ToString().TrimEnd();
    }

    protected override string Identifier => "HTTP mock";
}

/// <summary>
/// Assertions for RequestCollection.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name",
    Justification = "Multiple assertion classes in one file for convenience")]
public class RequestCollectionAssertions : GenericCollectionAssertions<CapturedRequest>
{
    private readonly RequestCollection subject;

    public RequestCollectionAssertions(RequestCollection subject)
#if FA8
        : base(subject, AssertionChain.GetOrCreate())
#else
        : base(subject)
#endif
    {
        this.subject = subject;
    }

    /// <summary>
    /// Asserts that the request collection does not contain any unexpected requests.
    /// </summary>
    public AndConstraint<RequestCollectionAssertions> NotContainUnexpectedCalls(string because = "", params object[] becauseArgs)
    {
        var unexpectedRequests = subject.Where(r => !r.WasExpected).ToList();

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(!unexpectedRequests.Any())
            .FailWith(
                "no unexpected requests should exist, but found {0} unexpected request(s):{1}{2}",
                unexpectedRequests.Count,
                Environment.NewLine,
                string.Join(Environment.NewLine, unexpectedRequests.Select(r => $"  {r.Method} {r.Uri}")));

        return new AndConstraint<RequestCollectionAssertions>(this);
    }

    /// <summary>
    /// Asserts that the collection contains at least one request and returns assertions for that request.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public ContainedRequestAssertions ContainRequest(string because = "", params object[] becauseArgs)
    {
#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(subject.Any())
            .FailWith("Expected at least one request to have been captured{because}, but none were found");

        return new ContainedRequestAssertions(subject.ToArray());
    }

    /// <summary>
    /// Asserts that the collection contains a request for the given URI and returns assertions for that request.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public ContainedRequestAssertions ContainRequestFor(Uri uri, string because = "", params object[] becauseArgs)
    {
        return ContainRequestFor(uri.ToString(), because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the collection contains a request for the given URL pattern and returns assertions for that request.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public ContainedRequestAssertions ContainRequestFor(string urlPattern, string because = "", params object[] becauseArgs)
    {
        CapturedRequest[] matchingRequests = subject
            .Where(r => r.Uri is not null && r.Uri.ToString().MatchesWildcard(urlPattern))
            .ToArray();

        var failureMessage = new StringBuilder();

        if (subject.Count == 0)
        {
            failureMessage.AppendFormat(
                "Expected a request for URL pattern \"{0}\"{{because}}, but no requests where captured at all", urlPattern);
        }
        else if (matchingRequests.Length == 0)
        {
            failureMessage.AppendFormat("Expected a request for URL pattern \"{0}\"{{because}}, but none were found among:",
                urlPattern);

            failureMessage.AppendLine();
            foreach (CapturedRequest request in subject)
            {
                failureMessage.AppendLine($" - {request}");
            }
        }
        else
        {
            // The assertion succeeded
        }

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(matchingRequests.Length > 0)
            .FailWith(failureMessage.ToString());

        return new ContainedRequestAssertions(matchingRequests);
    }

    /// <summary>
    /// Asserts that the collection does not contain a request matching the given URI.
    /// </summary>
    public AndConstraint<RequestCollectionAssertions> NotContainRequestFor(Uri uri, string because = "",
        params object[] becauseArgs)
    {
        return NotContainRequestFor(uri.ToString(), because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the collection does not contain a request matching the given URL pattern.
    /// </summary>
    public AndConstraint<RequestCollectionAssertions> NotContainRequestFor(string urlPattern, string because = "",
        params object[] becauseArgs)
    {
        var matches = subject.Where(r => r.Uri is not null && r.Uri.ToString().MatchesWildcard(urlPattern)).ToList();

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(!matches.Any())
            .FailWith(
                matches.Any()
                    ? $"Did not expect a request for URL pattern \"{urlPattern}\"{{because}}, but found:{Environment.NewLine}{string.Join(Environment.NewLine, matches.Select(r => $" - {r}"))}"
                    : $"Did not expect a request for URL pattern \"{urlPattern}\"{{because}}, but none were found")
            ;

        return new AndConstraint<RequestCollectionAssertions>(this);
    }
}

/// <summary>
/// Assertions for CapturedRequest.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name",
    Justification = "Multiple assertion classes in one file for convenience")]
public class CapturedRequestAssertions : ObjectAssertions<CapturedRequest, CapturedRequestAssertions>
{
    private readonly CapturedRequest subject;

    public CapturedRequestAssertions(CapturedRequest subject)
#if FA8
        : base(subject, AssertionChain.GetOrCreate())
#else
        : base(subject)
#endif
    {
        this.subject = subject;
    }

    /// <summary>
    /// Asserts that the request was expected (matched a mock).
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndConstraint<CapturedRequestAssertions> BeExpected(string because = "", params object[] becauseArgs)
    {
#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(subject.WasExpected)
            .FailWith("request should be expected, but it was unexpected");

        return new AndConstraint<CapturedRequestAssertions>(this);
    }

    /// <summary>
    /// Asserts that the request was unexpected (did not match any mock).
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndConstraint<CapturedRequestAssertions> BeUnexpected(string because = "", params object[] becauseArgs)
    {
#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(!subject.WasExpected)
            .FailWith("request should be unexpected, but it was expected");

        return new AndConstraint<CapturedRequestAssertions>(this);
    }

    protected override string Identifier
    {
        get => "request";
    }
}

/// <summary>
/// Assertion chain for a specific captured request located from a RequestCollection.
/// </summary>
public class ContainedRequestAssertions : ReferenceTypeAssertions<CapturedRequest, ContainedRequestAssertions>
{
    private readonly CapturedRequest[] requests;

    // Internal factory ctor: multiple requests (not part of public API)
    internal ContainedRequestAssertions(CapturedRequest[] requests)
#if FA8
        : base(requests.Length > 0 ? requests[0] : throw new ArgumentException("requests cannot be empty", nameof(requests)),
            AssertionChain.GetOrCreate())
#else
        : base(requests.Length > 0 ? requests[0] : throw new ArgumentException("requests cannot be empty", nameof(requests)))
#endif
    {
        this.requests = requests;
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has a header with the specified name (regardless of value).
    /// </summary>
    /// <remarks>
    /// To assert both the header name and a value pattern, use the overload that accepts a <c>valuePattern</c>
    /// parameter. When calling with two string arguments, use the named parameter syntax to avoid overload ambiguity:
    /// <c>WithHeader("name", valuePattern: "pattern*")</c>.
    /// </remarks>
    /// <param name="name">The name of the HTTP request header.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithHeader(string name, string because = "",
        params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            if (request.Headers.TryGetValues(name, out _))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        if (requests.Length == 1)
        {
            string presentHeaders = string.Join(", ", requests[0].Headers.Select(h => h.Key));
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected request to have header {0}{because}, but found: {1}", name,
                    string.IsNullOrEmpty(presentHeaders) ? "<no headers>" : presentHeaders);
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected at least one request to have header {0}{because}, but none did", name);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has a header with the specified name and a value
    /// matching the given wildcard pattern.
    /// </summary>
    /// <param name="name">The name of the HTTP request header.</param>
    /// <param name="valuePattern">
    /// The wildcard pattern to match against the header value. Use <c>*</c> as wildcard.
    /// </param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithHeader(string name, string valuePattern,
        string because = "",
        params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            if (request.Headers.TryGetValues(name, out IEnumerable<string>? values) &&
                values.Any(v => v.MatchesWildcard(valuePattern)))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        if (requests.Length == 1)
        {
            string actualValues = requests[0].Headers.TryGetValues(name, out IEnumerable<string>? actual)
                ? string.Join(", ", actual)
                : "<missing>";

#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected request header {0} to match wildcard pattern {1}{because}, but it was {2}", name, valuePattern,
                    actualValues);
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected at least one request to have header {0} matching wildcard pattern {1}{because}, but none did", name,
                    valuePattern);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has an <c>Authorization: Bearer</c> header.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBearerToken(string because = "",
        params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            if (string.Equals(request.Headers.Authorization?.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        if (requests.Length == 1)
        {
            var auth0 = requests[0].Headers.Authorization;
            string actual = auth0 is not null
                ? $"Authorization: {auth0.Scheme}"
                : "<no Authorization header>";

#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected request to have a Bearer token{because}, but found {0}", actual);
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected at least one request to have a Bearer token{because}, but none did");
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has an <c>Authorization: Bearer</c> header with a token
    /// matching the given wildcard pattern.
    /// </summary>
    /// <param name="tokenPattern">
    /// The wildcard pattern to match against the Bearer token value. Use <c>*</c> as wildcard.
    /// </param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBearerToken(string tokenPattern,
        string because = "",
        params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            var auth = request.Headers.Authorization;
            if (auth is not null &&
                string.Equals(auth.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
                auth.Parameter is not null &&
                auth.Parameter.MatchesWildcard(tokenPattern))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        if (requests.Length == 1)
        {
            var auth = requests[0].Headers.Authorization;
            string actual = auth is null
                ? "<no Authorization header>"
                : !string.Equals(auth.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
                    ? $"Authorization: {auth.Scheme}"
                    : $"Bearer {auth.Parameter ?? "<no token>"}";

#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected request to have a Bearer token matching {0}{because}, but found {1}", tokenPattern, actual);
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected at least one request to have a Bearer token matching {0}{because}, but none did", tokenPattern);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that the body of at least one of the matching requests matches a wildcard pattern.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBody(string wildcard, string because = "",
        params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            if (request.Body is not null && request.Body.MatchesWildcard(wildcard))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        if (requests.Length == 1)
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected request body to match wildcard pattern {0}, but it was {1}", wildcard,
                    requests[0].Body ?? "<null>");
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected at least one request having a body that matches wildcard pattern {0}, but none did",
                    wildcard);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has a body matching the provided JSON, ignoring whitespace/layout differences.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBodyMatchingJson(string json, string because = "",
        params object[] becauseArgs)
    {
        if (string.IsNullOrEmpty(json))
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .FailWith("Cannot compare the JSON body with <null>");
        }

        // Single-request behavior: keep original semantics/messages
        if (requests.Length == 1)
        {
            try
            {
                using var expected = JsonDocument.Parse(json);
                using var actual = JsonDocument.Parse(requests[0].Body ?? string.Empty);

#if FA8
                AssertionChain.GetOrCreate()
#else
                Execute.Assertion
#endif
                    .BecauseOf(because, becauseArgs)
                    .ForCondition(expected.RootElement.JsonEquals(actual.RootElement))
                    .FailWith("Expected request body to be JSON-equivalent to:{1}{0}{1}but was:{1}{2}", json, Environment.NewLine,
                        requests[0].Body ?? "<null>");
            }
            catch (JsonException)
            {
#if FA8
                AssertionChain.GetOrCreate()
#else
                Execute.Assertion
#endif
                    .FailWith("Request body is not valid JSON: {0}", requests[0].Body ?? "<null>");
            }

            return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
        }

        // Multiple requests: succeed if any body JSON-matches
        using var expectedDoc = JsonDocument.Parse(json);
        foreach (var request in requests)
        {
            try
            {
                using var actualDoc = JsonDocument.Parse(request.Body ?? string.Empty);
                if (expectedDoc.RootElement.JsonEquals(actualDoc.RootElement))
                {
                    return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
                }
            }
            catch (JsonException)
            {
                // Ignore invalid JSON bodies when multiple requests are present; we'll fail after checking all
            }
        }

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected at least one request body to be JSON-equivalent to:{1}{0}", json, Environment.NewLine);

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Deserializes the JSON request body to a particular type and asserts it is equivalent to the expected value.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBodyEquivalentTo<T>(T expected,
        string because = "",
        params object[] becauseArgs)
    {
        string[] leastFailures = [];
        CapturedRequest? bestMatch = null;
        foreach (CapturedRequest request in requests)
        {
            T? actual = request.Body is null ? default : JsonSerializer.Deserialize<T>(request.Body!);
            if (actual is not null)
            {
                using var scope = new AssertionScope();
                actual.Should().BeEquivalentTo(expected, because, becauseArgs);

                var failures = scope.Discard();
                if (failures.Length == 0)
                {
                    return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
                }

                if (leastFailures.Length == 0 || failures.Length < leastFailures.Length)
                {
                    leastFailures = failures;
                    bestMatch = request;
                }
            }
        }

        string message;
        if (requests.Length == 1)
        {
            message = "Expected request #{0} ({1}) to have a body equivalent to the expectation{because}, but it did not:";
        }
        else
        {
            message =
                "Expected the closest matching request #{0} ({1}) at have a body equivalent to the expectation{because}, but it did not:";
        }

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .FailWith(message +
                      Environment.NewLine +
                      string.Join(Environment.NewLine, leastFailures.Select(failure => $"- {failure}")) +
                      Environment.NewLine,
                bestMatch!.Sequence, bestMatch);

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Deserializes the request body as a dictionary and asserts that it contains at least the properties of the
    /// expected dictionary with matching values. Additional properties in the request body are ignored.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBodyHavingPropertiesOf(
        IDictionary<string, string> expectation,
        string because = "",
        params object[] becauseArgs)
    {
        string[] failures = [];
        foreach (CapturedRequest request in requests)
        {
            if (request.Body is null)
            {
                continue;
            }

            var dictionary = JsonSerializer.Deserialize<IDictionary<string, object?>>(request.Body);
            if (dictionary is not null)
            {
                var actual = dictionary
                    .Where(x => expectation.ContainsKey(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value?.ToString());

                using var scope = new AssertionScope();
                actual.Should().BeEquivalentTo(expectation, because, becauseArgs);

                failures = scope.Discard();
                if (failures.Length == 0)
                {
                    return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
                }
            }
        }

        if (requests.Length == 1)
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected the top-level properties of the request body to be equivalent to the provided dictionary{because}, but it failed with: ",
                    string.Join(Environment.NewLine, failures));
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected at least one request body to have the expected properties{because}, but none did");
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Deserializes the request body as a dictionary and asserts it is exactly equivalent to the expected dictionary,
    /// without any additional properties.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBodyHavingPropertiesEqualTo(
        IDictionary<string, string> expectation,
        string because = "",
        params object[] becauseArgs)
    {
        string[] failures = [];
        foreach (CapturedRequest request in requests)
        {
            if (request.Body is null)
            {
                continue;
            }

            var dictionary = JsonSerializer.Deserialize<IDictionary<string, object?>>(request.Body);
            if (dictionary is not null)
            {
                var actual = dictionary.ToDictionary(x => x.Key, x => x.Value?.ToString());

                using var scope = new AssertionScope();
                actual.Should().BeEquivalentTo(expectation, because, becauseArgs);

                failures = scope.Discard();
                if (failures.Length == 0)
                {
                    return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
                }
            }
        }

        if (requests.Length == 1)
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected the top-level properties of the request body to be equivalent to the provided dictionary{because}, but it failed with: ",
                    string.Join(Environment.NewLine, failures));
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected at least one request body to have the expected properties{because}, but none did");
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts the body contains a top-level property with the given key and value.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithBodyHavingProperty(string key, string value,
        string because = "",
        params object[] becauseArgs)
    {
        string[] failures = [];
        foreach (CapturedRequest request in requests)
        {
            if (request.Body is null)
            {
                continue;
            }

            var dictionary = JsonSerializer.Deserialize<IDictionary<string, object?>>(request.Body);
            if (dictionary is not null)
            {
                var actual = dictionary.ToDictionary(x => x.Key, x => x.Value?.ToString());

                using var scope = new AssertionScope();
                actual.Should().Contain(key, value, because, becauseArgs);

                failures = scope.Discard();
                if (failures.Length == 0)
                {
                    return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
                }
            }
        }

        if (requests.Length == 1)
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected the request body to contain property {0} with value {1}{because}, but it did not: {2}", key,
                    value,
                    string.Join(Environment.NewLine, failures));
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected at least one request body to contain property {0} with value {1}{because}, but none did", key,
                    value);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has a query parameter with the specified name (any value).
    /// </summary>
    /// <remarks>
    /// To avoid C# overload-resolution ambiguity with the value-pattern overload, this overload intentionally omits
    /// <c>because</c> / <c>becauseArgs</c> parameters. Use
    /// <see cref="WithQueryParam(string, string, string, object[])"/> with <c>valuePattern: "*"</c> if you need a
    /// failure reason.
    /// </remarks>
    /// <param name="name">The name of the query parameter to look for.</param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithQueryParam(string name)
    {
        foreach (CapturedRequest request in requests)
        {
            if (ParseUrlEncodedPairs(request.Query).Any(p => p.Name == name))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        if (requests.Length == 1)
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .FailWith(
                    "Expected request to have a query parameter named {0}, but the query string was: {1}",
                    name, requests[0].Query);
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .FailWith(
                    "Expected at least one request to have a query parameter named {0}, but none did", name);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has a query parameter with the specified name and a value
    /// matching a wildcard pattern.
    /// </summary>
    /// <param name="name">The name of the query parameter to look for.</param>
    /// <param name="valuePattern">A wildcard pattern the parameter value must match. Use <c>*</c> as a wildcard.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithQueryParam(
        string name, string valuePattern, string because = "", params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            foreach (var (paramName, paramValue) in ParseUrlEncodedPairs(request.Query))
            {
                if (paramName == name && paramValue.MatchesWildcard(valuePattern))
                {
                    return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
                }
            }
        }

        if (requests.Length == 1)
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected request to have a query parameter {0} matching wildcard {1}{because}, but the query string was: {2}",
                    name, valuePattern, requests[0].Query);
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected at least one request to have a query parameter {0} matching wildcard {1}{because}, but none did",
                    name, valuePattern);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests received a response containing the specified header.
    /// </summary>
    /// <param name="name">The name of the response header to look for.</param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithResponseHeader(string name)
    {
        foreach (CapturedRequest request in requests)
        {
            if (HasResponseHeader(request.Response, name))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        string message = requests.Length == 1
            ? "Expected response to contain header {0}, but it was not found"
            : "Expected at least one response to contain header {0}, but none did";

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .FailWith(message, name);

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests received a response with the specified header
    /// whose value matches the given wildcard pattern.
    /// </summary>
    /// <param name="name">The name of the response header.</param>
    /// <param name="value">A wildcard pattern the header value must match. Use <c>*</c> as wildcard character.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithResponseHeader(
        string name, string value, string because = "", params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            if (HasResponseHeaderWithValue(request.Response, name, value))
            {
                return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
            }
        }

        string message = requests.Length == 1
            ? "Expected response header {0} to match wildcard pattern {1}{because}, but it did not"
            : "Expected at least one response to have header {0} matching wildcard pattern {1}{because}, but none did";

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .FailWith(message, name, value);

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    /// <summary>
    /// Asserts that at least one of the matching requests has a URL-encoded form field with the specified name and a
    /// value matching a wildcard pattern.
    /// </summary>
    /// <param name="name">The name of the form field to look for.</param>
    /// <param name="valuePattern">A wildcard pattern the field value must match. Use <c>*</c> as a wildcard.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// A construct that allows chaining more assertions on the matching <see cref="CapturedRequest"/>
    /// </returns>
    public AndWhichConstraint<ContainedRequestAssertions, CapturedRequest> WithFormField(
        string name, string valuePattern, string because = "", params object[] becauseArgs)
    {
        foreach (CapturedRequest request in requests)
        {
            if (request.Body is null)
            {
                continue;
            }

            foreach (var (fieldName, fieldValue) in ParseUrlEncodedPairs(request.Body))
            {
                if (fieldName == name && fieldValue.MatchesWildcard(valuePattern))
                {
                    return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, request);
                }
            }
        }

        if (requests.Length == 1)
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected request to have a form field {0} matching wildcard {1}{because}, but the body was: {2}",
                    name, valuePattern, requests[0].Body ?? "<null>");
        }
        else
        {
#if FA8
            AssertionChain.GetOrCreate()
#else
            Execute.Assertion
#endif
                .BecauseOf(because, becauseArgs)
                .FailWith(
                    "Expected at least one request to have a form field {0} matching wildcard {1}{because}, but none did",
                    name, valuePattern);
        }

        return new AndWhichConstraint<ContainedRequestAssertions, CapturedRequest>(this, []);
    }

    private static IEnumerable<(string Name, string Value)> ParseUrlEncodedPairs(string? rawQuery)
    {
        if (rawQuery is null or { Length: 0 })
        {
            yield break;
        }

        string query = rawQuery.TrimStart('?');

        foreach (string pair in query.Split('&'))
        {
            if (string.IsNullOrEmpty(pair))
            {
                continue;
            }

            int idx = pair.IndexOf("=", StringComparison.Ordinal);

            if (idx < 0)
            {
                yield return (WebUtility.UrlDecode(pair), string.Empty);
            }
            else
            {
                yield return (WebUtility.UrlDecode(pair[..idx]), WebUtility.UrlDecode(pair[(idx + 1)..]));
            }
        }
    }

    private static bool HasResponseHeader(HttpResponseMessage response, string name)
    {
        try
        {
            if (response.Headers.Contains(name))
            {
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Header name belongs to a different header category (e.g., content header); check below.
        }

        try
        {
            return response.Content?.Headers.Contains(name) == true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool HasResponseHeaderWithValue(HttpResponseMessage response, string name, string valuePattern)
    {
        try
        {
            if (response.Headers.TryGetValues(name, out var headerValues))
            {
                return headerValues.Any(v => v.MatchesWildcard(valuePattern));
            }
        }
        catch (InvalidOperationException)
        {
            // Header name belongs to a different header category (e.g., content header); check below.
        }

        try
        {
            if (response.Content?.Headers.TryGetValues(name, out var contentHeaderValues) == true)
            {
                return contentHeaderValues!.Any(v => v.MatchesWildcard(valuePattern));
            }
        }
        catch (InvalidOperationException)
        {
            // Header name is not valid for either header collection.
        }

        return false;
    }

    protected override string Identifier
    {
        get => "captured request";
    }
}

/// <summary>
/// Assertions for <see cref="RequestMockResponseBuilder"/>, enabling per-mock invocation count assertions.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name",
    Justification = "Multiple assertion classes in one file for convenience")]
public class RequestMockResponseBuilderAssertions(RequestMockResponseBuilder subject) :
#if FA8
    ReferenceTypeAssertions<RequestMockResponseBuilder, RequestMockResponseBuilderAssertions>(subject, AssertionChain.GetOrCreate())
#else
    ReferenceTypeAssertions<RequestMockResponseBuilder, RequestMockResponseBuilderAssertions>(subject)
#endif
{
    private readonly RequestMockResponseBuilder subject = subject;

    /// <summary>
    /// Asserts that this mock has been invoked at least once.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndConstraint<RequestMockResponseBuilderAssertions> HaveBeenCalled(string because = "",
        params object[] becauseArgs)
    {
        int count = subject.RequestMock.InvocationCount;

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(count >= 1)
            .FailWith(
                "Expected mock {0} {1} to have been called at least once{because}, but it was not called at all.",
                subject.RequestMock.Method,
                string.IsNullOrEmpty(subject.RequestMock.PathPattern) ? "(any path)" : subject.RequestMock.PathPattern);

        return new AndConstraint<RequestMockResponseBuilderAssertions>(this);
    }

    /// <summary>
    /// Asserts that this mock has been invoked exactly <paramref name="times"/> times.
    /// </summary>
    /// <param name="times">The exact number of times the mock is expected to have been invoked. Must be zero or greater.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndConstraint<RequestMockResponseBuilderAssertions> HaveBeenCalled(int times, string because = "",
        params object[] becauseArgs)
    {
        if (times < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(times), times, "Expected invocation count cannot be negative.");
        }

        int count = subject.RequestMock.InvocationCount;

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(count == times)
            .FailWith(
                "Expected mock {0} {1} to have been called exactly {2} time(s){because}, but it was called {3} time(s).",
                subject.RequestMock.Method,
                string.IsNullOrEmpty(subject.RequestMock.PathPattern) ? "(any path)" : subject.RequestMock.PathPattern,
                times,
                count);

        return new AndConstraint<RequestMockResponseBuilderAssertions>(this);
    }

    /// <summary>
    /// Asserts that this mock has not been invoked.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public AndConstraint<RequestMockResponseBuilderAssertions> NotHaveBeenCalled(string because = "",
        params object[] becauseArgs)
    {
        int count = subject.RequestMock.InvocationCount;

#if FA8
        AssertionChain.GetOrCreate()
#else
        Execute.Assertion
#endif
            .BecauseOf(because, becauseArgs)
            .ForCondition(count == 0)
            .FailWith(
                "Expected mock {0} {1} to not have been called{because}, but it was called {2} time(s).",
                subject.RequestMock.Method,
                string.IsNullOrEmpty(subject.RequestMock.PathPattern) ? "(any path)" : subject.RequestMock.PathPattern,
                count);

        return new AndConstraint<RequestMockResponseBuilderAssertions>(this);
    }

    /// <inheritdoc/>
    protected override string Identifier => "request mock";
}
