// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace DuraFunApp
{
    public static class HelloSequence
    {
        [FunctionName("E1_HelloSequence")]
        public static async Task<List<string>> Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Tokyo"));
            context.SetCustomStatus("Tokyo");

            DateTime deadline = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(3));
            await context.CreateTimer(deadline, CancellationToken.None);

            outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Seattle"));
            context.SetCustomStatus("Tokyo, Seattle");

            await context.CreateTimer(deadline, CancellationToken.None);

            outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "London"));
            context.SetCustomStatus("Tokyo, Seattle, London");

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("E1_SayHello")]
        public static string SayHello([ActivityTrigger] string name)
        {
            return $"Hello {name}!";
        }
    }

}
