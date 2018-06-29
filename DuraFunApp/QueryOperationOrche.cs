// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DuraFunApp
{
    public static class QueryOperationOrche
    {

        [FunctionName("QueryOperation")]
        public static async Task<string> Run(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            string result = string.Empty;
            string requestID = context.GetInput<string>()?.Trim();
            if(string.IsNullOrEmpty(requestID))
            {
                requestID = Guid.NewGuid().ToString();
            }

            var actvityInfo = new ActivityCreateBlobInfo(){
                BlobContainer = requestID,
                NumberOfRecords = 100,
            };

            int loopCount = 10;
            var tasks = new List<Task<string>>();
            for (int i = 0; i < loopCount; i++)
            {
                actvityInfo.BlobName = $"{i+1}.json";
                actvityInfo.NumberToStart = i * actvityInfo.NumberOfRecords + 1;

                tasks.Add(
                    context.CallActivityAsync<string>(
                        "WriteRecordsToBlob",
                        actvityInfo)
                    );
            }

            //case using WhenAll
            string[] results = await Task.WhenAll(tasks);
                    //.ConfigureAwait(false); 사용하면 안됨. 
                    // 기본값 true로 해서 SynchronizationContext로 동기화하지 않는다면, 끝났다는 정보를 API가 제공하지 못함.
                    // 즉, 실행이 끝났는대도 API는 running을 알려주게 됨.

            result = JsonConvert.SerializeObject(results);

            #region reference for WhenAny
            //Case using WhenAny
            //while (tasks.Count > 0)
            //{
            //    var finishedTask = await Task.WhenAny(tasks);
            //    if (finishedTask.Status == TaskStatus.RanToCompletion)
            //    {

            //    }
            //    result += finishedTask.Result;
            //    context.SetCustomStatus(result);

            //    tasks.Remove(finishedTask);
            //}

            //Action<Task<string>> handler = null;
            //handler = t =>
            //{
            //    if (t.IsFaulted)
            //    {
            //        tasks.Remove(t);
            //        if (tasks.Count == 0)
            //        {
            //            throw new Exception("No Tasks at all!");
            //        }
            //        Task.Factory.ContinueWhenAny(tasks.ToArray(), handler);
            //    }
            //    else
            //    {
            //        //result += t.Result;     //not work
            //        //context.SetCustomStatus(result);    //not work
            //        //Console.WriteLine($"Task Result : {t.Result}");
            //    }
            //};

            //Task.Factory.ContinueWhenAny(tasks.ToArray(), handler);

            //for (int i = 0; i < loopCount; i++)
            //{
            //    result += await tasks[0];
            //}
            #endregion

            return result;
        }

        [FunctionName("WriteRecordsToBlob")]
        public static async Task<string> WriteRecordsToBlob(
            [ActivityTrigger] ActivityCreateBlobInfo info,
            Binder binder,
            ILogger log)
        {
            string outputLocation = $"{info.BlobContainer}/{info.BlobName}";

            List<FakeDTO> list = PopulateFakeData(info);

            log.LogInformation($"[INFO] Started from '{info.NumberToStart} to get {info.NumberOfRecords} records'");
            log.LogInformation($"[INFO] Writing to '{outputLocation}'");

            // write contents into a blob
            JsonSerializer serializer = new JsonSerializer();
            try
            {
                using (TextWriter outputWriter = await binder.BindAsync<TextWriter>(
                        new BlobAttribute(outputLocation, FileAccess.Write)))
                {
                    serializer.Serialize(outputWriter, list);
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error : '{ex.Message}'");
            }

            return outputLocation;
        }

        private static List<FakeDTO> PopulateFakeData(ActivityCreateBlobInfo info)
        {
            var list = new List<FakeDTO>();
            for (int i = info.NumberToStart; i < info.NumberToStart + info.NumberOfRecords; i++)
            {
                list.Add(new FakeDTO()
                {
                    SeqID = Guid.NewGuid().ToString(),
                    Name = "Name" + i.ToString(),
                    TopValue = "Top" + i.ToString(),
                    BottomValue = "Bottom" + i.ToString()
                });
            }

            return list;
        }
    }

    public class ActivityCreateBlobInfo
    {
        public string BlobContainer { get; set; }
        public string BlobName { get; set; }

        public int NumberToStart { get; set; }
        public int NumberOfRecords { get; set; }
    }

    public class FakeDTO
    {
        public string SeqID { get; set; }
        public string Name { get; set; }
        public string TopValue { get; set; }

        public string BottomValue { get; set; }
    }
}


