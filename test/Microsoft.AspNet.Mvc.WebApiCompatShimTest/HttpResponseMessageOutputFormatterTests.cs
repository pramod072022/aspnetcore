﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !ASPNETCORE50

using System;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.AspNet.PipelineCore;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShimTest
{
    public class HttpResponseMessageOutputFormatterTests
    {
        [Fact]
        public async Task Disposed_CalledOn_HttpResponseMessage()
        {
            // Arrange
            var formatter = new HttpResponseMessageOutputFormatter();
            var streamContent = new Mock<StreamContent>(new MemoryStream());
            streamContent.Protected().Setup("Dispose", true).Verifiable();
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = streamContent.Object;
            var outputFormatterContext = GetOutputFormatterContext(
                                                httpResponseMessage,
                                                typeof(HttpResponseMessage),
                                                new DefaultHttpContext());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            streamContent.Protected().Verify("Dispose", Times.Once(), true);
        }

        [Fact]
        public async Task ExplicitlySet_ChunkedEncodingFlag_IsIgnored()
        {
            // Arrange
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World")));
            httpResponseMessage.Headers.TransferEncodingChunked = true;

            var httpContext = new DefaultHttpContext();
            var formatter = new HttpResponseMessageOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(
                                                httpResponseMessage,
                                                typeof(HttpResponseMessage),
                                                httpContext);
            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.False(httpContext.Response.Headers.ContainsKey("Transfer-Encoding"));
            Assert.NotNull(httpContext.Response.ContentLength);
        }

        [Fact]
        public async Task ExplicitlySet_ChunkedEncodingHeader_IsIgnored()
        {
            // Arrange
            var transferEncodingHeaderKey = "Transfer-Encoding";
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World")));
            httpResponseMessage.Headers.Add(transferEncodingHeaderKey, "chunked");

            var httpContext = new DefaultHttpContext();
            var formatter = new HttpResponseMessageOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(
                                                httpResponseMessage,
                                                typeof(HttpResponseMessage),
                                                httpContext);
            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.False(httpContext.Response.Headers.ContainsKey(transferEncodingHeaderKey));
            Assert.NotNull(httpContext.Response.ContentLength);
        }

        [Fact]
        public async Task ExplicitlySet_MultipleEncodings_ChunkedNotIgnored()
        {
            // Arrange
            var transferEncodingHeaderKey = "Transfer-Encoding";
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World")));
            httpResponseMessage.Headers.Add(transferEncodingHeaderKey, new[] { "identity", "chunked" });

            var httpContext = new DefaultHttpContext();
            var formatter = new HttpResponseMessageOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(
                                                httpResponseMessage,
                                                typeof(HttpResponseMessage),
                                                httpContext);
            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.True(httpContext.Response.Headers.ContainsKey(transferEncodingHeaderKey));
            Assert.Equal(new string[] { "identity", "chunked" }, 
                        httpContext.Response.Headers.GetValues(transferEncodingHeaderKey));
            Assert.NotNull(httpContext.Response.ContentLength);
        }

        [Fact]
        public async Task ExplicitlySet_MultipleEncodingsUsingChunkedFlag_ChunkedNotIgnored()
        {
            // Arrange
            var transferEncodingHeaderKey = "Transfer-Encoding";
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Hello, World")));
            httpResponseMessage.Headers.Add(transferEncodingHeaderKey, new[] { "identity" });
            httpResponseMessage.Headers.TransferEncodingChunked = true;

            var httpContext = new DefaultHttpContext();
            var formatter = new HttpResponseMessageOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(
                                                httpResponseMessage,
                                                typeof(HttpResponseMessage),
                                                httpContext);
            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.True(httpContext.Response.Headers.ContainsKey(transferEncodingHeaderKey));
            Assert.Equal(new string[] { "identity", "chunked" },
                        httpContext.Response.Headers.GetValues(transferEncodingHeaderKey));
            Assert.NotNull(httpContext.Response.ContentLength);
        }

        private OutputFormatterContext GetOutputFormatterContext(object outputValue, Type outputType,
                                                                    HttpContext httpContext)
        {
            return new OutputFormatterContext
            {
                Object = outputValue,
                DeclaredType = outputType,
                ActionContext = new ActionContext(httpContext, routeData: null, actionDescriptor: null)
            };
        }
    }
}
#endif