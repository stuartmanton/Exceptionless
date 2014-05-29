﻿#region Copyright 2014 Exceptionless

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
//     http://www.apache.org/licenses/LICENSE-2.0

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Submission.Net;

namespace Exceptionless.Submission {
    public class DefaultSubmissionClient : ISubmissionClient {
        public Task<SubmissionResponse> SubmitAsync(IEnumerable<Event> events, ExceptionlessConfiguration configuration) {
            HttpWebRequest client = WebRequest.CreateHttp(String.Concat(configuration.GetServiceEndPoint(), "event"));
            client.SetUserAgent(configuration.UserAgent);
            client.AddAuthorizationHeader(configuration);

            // TODO: We only support one error right now..
            var serializer = configuration.Resolver.GetJsonSerializer();
            var data = serializer.Serialize(events.FirstOrDefault());

            return client.PostJsonAsync(data).ContinueWith(t => {
                // TODO: We need to break down the aggregate exceptions error message into something usable.
                if (t.IsFaulted || t.IsCanceled || t.Exception != null)
                    return new SubmissionResponse(false, exception: t.Exception, message: t.Exception.GetMessage());

                var response = (HttpWebResponse)t.Result;

                int settingsVersion;
                if (!Int32.TryParse(response.Headers[ExceptionlessHeaders.ConfigurationVersion], out settingsVersion))
                    settingsVersion = -1;

                // TODO: Add support for sending errors later (e.g., Suspended Account. Invalid API Key).

                if (response.StatusCode != HttpStatusCode.OK)
                    return new SubmissionResponse(false, settingsVersion, message: response.GetResponseText());

                return new SubmissionResponse(true, settingsVersion);
            });
        }

        public Task<SettingsResponse> GetSettingsAsync(ExceptionlessConfiguration configuration) {
            HttpWebRequest client = WebRequest.CreateHttp(String.Concat(configuration.GetServiceEndPoint(), "project/config"));
            client.AddAuthorizationHeader(configuration);
            
            return client.GetJsonAsync().ContinueWith(t => {
                if (t.IsFaulted || t.IsCanceled || t.Exception != null)
                    return new SettingsResponse(false, exception: t.Exception, message: t.Exception.GetMessage());

                var response = (HttpWebResponse)t.Result;
                if (response.StatusCode != HttpStatusCode.OK)
                    return new SettingsResponse(false, message: "Unable to retrieve configuration settings.");

                var json = response.GetResponseText();
                if (String.IsNullOrWhiteSpace(json))
                    return new SettingsResponse(false, message: "Invalid configuration settings.");

                var serializer = DependencyResolver.Default.GetJsonSerializer();
                try {
                    var settings = serializer.Deserialize<ClientConfiguration>(json);
                    return new SettingsResponse(true, settings.Settings, settings.Version);
                } catch (Exception ex) {
                    var message = String.Format("Unable to deserialize configuration settings. Exception: {0}", ex.Message);
                    return new SettingsResponse(false, message: message);
                }
            });
        }
    }
}