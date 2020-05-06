using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace DXS.RebaseChecker
{
    public static class PullRequestTrigger
    {
        [FunctionName("PullRequestTrigger")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string baseUrl = data.resourceContainers.project.baseUrl.ToString();
            string projectName = data.resourceContainers.project.id.ToString();
            string repositoryName = data.resource.repository.id.ToString();
            string targetRefName = data.resource.targetRefName.ToString();
            string baseServiceUrl = string.Format("{0}{1}/_apis/git/repositories/{2}",
            baseUrl,
            projectName,
            repositoryName);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                        ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", Environment.GetEnvironmentVariable("pat")))));

                string targetCommitId = await GetCommitIdFromRefNameAsync(baseServiceUrl, targetRefName, client);
                dynamic pullRequests = await GetPullRequestByTargetRefNameAsync(baseServiceUrl, targetRefName, client);

                if(pullRequests != null)
                {
                    foreach(var dataPullRequest in pullRequests)
                    {
                        int pullRequestId = (int)dataPullRequest.pullRequestId;
                        string sourceRefName = dataPullRequest.sourceRefName.ToString();
                        string sourceCommitId = await GetCommitIdFromRefNameAsync(baseServiceUrl, sourceRefName, client);
                        string behindCount = await GetBehindCountAsync(baseServiceUrl, sourceCommitId, targetCommitId, client);
                        string serializedStatus = ComputePullRequestStatus(behindCount);
                        await UpdatePullRequestStatusAsync(baseServiceUrl, pullRequestId, client, serializedStatus);
                    }
                }
            }
            return new OkObjectResult("");
        }

        private static async Task<dynamic> GetPullRequestByTargetRefNameAsync(string baseServiceUrl, string targetRefName, HttpClient httpClient)
        {
            string requestUrl = string.Format("{0}/pullrequests?searchCriteria.targetRefName={1}&api-version=5.1",
            baseServiceUrl,
            targetRefName);
            var responseMessage = await httpClient.GetAsync(requestUrl);
            string content = await responseMessage.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(content);
            int pullRequestsCount = (int)data.count;
            if(pullRequestsCount > 0)
            {
                return data.value;
            }
            return null;
        }

        private static async Task<string> GetCommitIdFromRefNameAsync(string baseServiceUrl, string refName, HttpClient httpClient)
        {
            string requestUrl = string.Format("{0}/{1}",
            baseServiceUrl,
            refName);
            var responseMessage = await httpClient.GetAsync(requestUrl);
            string content = await responseMessage.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(content);
            return data.value[0].objectId.ToString();
        }

        private static async Task<string> GetBehindCountAsync(string baseServiceUrl, string sourceCommitId, string targetCommitId, HttpClient httpClient)
        {
            string requestUrl = string.Format("{0}/diffs/commits?diffCommonCommit=false&baseVersionType=commit&baseVersion={1}&targetVersionType=commit&targetVersion={2}",
            baseServiceUrl,
            targetCommitId,
            sourceCommitId);
            var responseMessage = await httpClient.GetAsync(requestUrl);
            var content = await responseMessage.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(content);
            return data.behindCount.ToString();
        }

        private static async Task UpdatePullRequestStatusAsync(string baseServiceUrl, int pullRequestId, HttpClient httpClient, string serializedStatus)
        {
            string requestUrl = string.Format("{0}/pullrequests/{1}/statuses?api-version=5.1-preview.1",
            baseServiceUrl,
            pullRequestId);
            await httpClient.PostAsync(requestUrl, new StringContent(serializedStatus, Encoding.UTF8, "application/json"));
        }

        private static string ComputePullRequestStatus(string behindCount)
        {
            string state = "succeeded";
            string description = "Rebased with target. " + DateTime.Now;

            if (behindCount != "0")
            {
                state = "failed";
                description = "Please rebase to target first. Behind: " + behindCount + ". " + DateTime.Now;
            }

            return JsonConvert.SerializeObject(
                new
                {
                    State = state,
                    Description = description,
                    Context = new
                    {
                        Name = "rebasechecker",
                        Genre = "checker"
                    }
                });
        }
    }
}