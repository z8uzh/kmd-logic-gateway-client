using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using YamlDotNet.Serialization;

namespace GatewayPublishing
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Enter the JWT token");
            var token = Console.ReadLine();
            var baseUrl = @"https://kmd-logic-api-shareddev-webapp.azurewebsites.net/";
            var subscriptionId = "ed710046-601c-45d0-ac02-ca9e7eb7c367";
            var providerId = "4cfafb0a-f458-405e-aa1d-60751b6c35cd";
            var yaml = new Deserializer().Deserialize<YamlContents>(File.OpenText(Path.Combine(Directory.GetCurrentDirectory(), @"Publish/publish.yml")));
            var client = AuthenticatedClient(baseUrl, token);
            var httpResponseMessagesForProducts = yaml.Products.Select(product => CreateProduct(client, subscriptionId, providerId, product)).ToList();
            var productResponses = httpResponseMessagesForProducts.Select(x => JsonConvert.DeserializeObject<ProductResponse>(x.Content.ReadAsStringAsync().Result)).ToList();
            var httpResponseMessagesForProductPolicies = productResponses
                .Select(product => ApplyPolicies(client, subscriptionId, product.Id.ToString(), yaml.Products.Where(x => x.Name == product.Name)
                .Select(x => x.PoliciesXmlFile).FirstOrDefault(), PolicyEntityType.Product)).ToList();
            var isErrorInProductsResponses = httpResponseMessagesForProducts.Select(x => x.IsSuccessStatusCode == false).FirstOrDefault();
            var isErrorInProductPolicyResponses = httpResponseMessagesForProductPolicies.Select(x => x.IsSuccessStatusCode == false).FirstOrDefault();
            var httpResponseMessagesForApis = CreateApis(client, subscriptionId, providerId, yaml.Apis, productResponses);
            var apiResponses = httpResponseMessagesForApis["apis"].Select(x => JsonConvert.DeserializeObject<ApiResponse>(x.Content.ReadAsStringAsync().Result)).ToList();

            string[] productIds = productResponses.Select(x => x.Id.ToString()).ToArray();
            string[] apiIds = apiResponses.Select(x => x.Id.ToString()).ToArray();

            string path = @"C:\Users\mr00564401\Desktop\Ids\Ids.txt";
            File.WriteAllLines(path, productIds, Encoding.UTF8);
            File.AppendAllLines(path, apiIds, Encoding.UTF8);


            //var isErrorInApiResponses = httpResponseMessagesForApis.Select(x => x.IsSuccessStatusCode == false).FirstOrDefault();

            //string[] prods = {
            //    "77c0ae40-b269-4796-b996-72023c7aa652",
            //    "56c96cba-7350-4657-b693-2b9896996620"
            //};
            //foreach (var productId in prods)
            //{
            //    DeleteProduct(client, subscriptionId, productId);
            //}

            //string[] apis = {
            //    "36274b2d-21f3-4210-99de-82ba13f81ff9",
            //    "05ee92a4-9352-488a-8bd8-99141f65da9a",
            //    "0ac27803-ba7c-4bee-a7a8-d3e3bc431e3c",
            //    "2171d296-b744-44ed-a637-e84e29461d83"
            //};
            //foreach (var apiId in apis)
            //{
            //    DeleteApi(client, subscriptionId, apiId);
            //}
        }

        private static HttpClient AuthenticatedClient(string baseUrl, string jwt)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            var contentType = new MediaTypeWithQualityHeaderValue("application/json");
            client.DefaultRequestHeaders.Accept.Add(contentType);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return client;
        }

        private static HttpResponseMessage ApplyPolicies(HttpClient client, string subscriptionId, string entityId, string xmlPath, PolicyEntityType policyEntityType)
        {
            var requestBody = new PolicyRequest { 
                EntityId = new Guid(entityId),
                Name = $"Policy-{entityId}",
                Description = "Policy for product through automation",
                Xml = GetXML(xmlPath),
                EntityType = policyEntityType
            };

            return client.PostAsync($"/subscriptions/{subscriptionId}/gateway/policies/custom", new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")).Result;
        }

        private static Dictionary<string, List<HttpResponseMessage>> CreateApis(HttpClient client, string subscriptionId, string providerId, List<Api> apis, List<ProductResponse> productResponses)
        {
            var httpResponseMessagesForApis = new List<HttpResponseMessage>();
            var httpResponseMessagesForApiRevisions = new List<HttpResponseMessage>();
            var httpResponseMessagesForApiPolicies = new List<HttpResponseMessage>();
            foreach (var api in apis)
            {
                if (api.ApiVersions.Count == 1)
                {
                    var httpResponseMessage = CreateApi(client, subscriptionId, providerId, api.Name, api.Path, api.ApiVersions.FirstOrDefault(), productResponses);
                    httpResponseMessagesForApis.Add(httpResponseMessage);
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(httpResponseMessage.Content.ReadAsStringAsync().Result);

                    var revisions = api.ApiVersions.FirstOrDefault().Revisions;
                    if (revisions != null)
                    {
                        var httpResponseMessageForRevisions = revisions.Select(x => CreateApiRevision(client, subscriptionId, apiResponse.Id.ToString(), x)).ToList();
                        httpResponseMessagesForApiRevisions.AddRange(httpResponseMessageForRevisions);
                    }

                    var policy = api.ApiVersions.FirstOrDefault().PoliciesXmlFile;
                    if (policy != null)
                    {
                        httpResponseMessagesForApiPolicies.Add(ApplyPolicies(client, subscriptionId, apiResponse.Id.ToString(), policy, PolicyEntityType.Api));
                    }
                }
                else if (api.ApiVersions.Count > 1)
                {
                    var httpResponseMessage = CreateApi(client, subscriptionId, providerId, api.Name, api.Path, api.ApiVersions.FirstOrDefault(), productResponses);
                    httpResponseMessagesForApis.Add(httpResponseMessage);
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(httpResponseMessage.Content.ReadAsStringAsync().Result);
                    foreach (var version in api.ApiVersions.Skip(1))
                    {
                        var httpResponseMessageForVersion = CreateApiVersion(client, subscriptionId, providerId, apiResponse.Name, apiResponse.Path, apiResponse.ApiVersionSetId.ToString(), version, productResponses);
                        httpResponseMessagesForApis.Add(httpResponseMessageForVersion);

                        var revisions = version.Revisions;
                        if (revisions != null)
                        {
                            var httpResponseMessageForRevisions = revisions.Select(x => CreateApiRevision(client, subscriptionId, apiResponse.Id.ToString(), x)).ToList();
                            httpResponseMessagesForApiRevisions.AddRange(httpResponseMessageForRevisions);
                        }

                        var policy = version.PoliciesXmlFile;
                        if (policy != null)
                        {
                            httpResponseMessagesForApiPolicies.Add(ApplyPolicies(client, subscriptionId, apiResponse.Id.ToString(), policy, PolicyEntityType.Api));
                        }
                    }
                }
            }

            return new Dictionary<string, List<HttpResponseMessage>> { 
                { "apis", httpResponseMessagesForApis },
                { "revisions", httpResponseMessagesForApiRevisions},
                { "policies", httpResponseMessagesForApiPolicies }
            };
        }

        private static HttpResponseMessage CreateProduct(HttpClient client, string subscriptionId, string providerId, Product product)
        {
            var requestBody = new MultipartFormDataContent
                {
                    { new StringContent(Guid.NewGuid().ToString(), Encoding.UTF8), "Id" },
                    { new StringContent(product.Name, Encoding.UTF8), "Name" },
                    { new StringContent(product.ApiKeyRequired, Encoding.UTF8), "ApiKeyRequired" },
                    { new StringContent(product.ProviderApprovalRequired, Encoding.UTF8), "ProviderApprovalRequired" },
                    { new StringContent(product.Description, Encoding.UTF8), "Description" },
                    { new StringContent(product.LegalTerms, Encoding.UTF8), "ProductTerms" },
                    { new StringContent(product.Published, Encoding.UTF8), "Publish" },
                    { new StringContent(providerId, Encoding.UTF8), "ProviderId" },
                };

            return client.PostAsync(new Uri($"/subscriptions/{subscriptionId}/gateway/products", UriKind.Relative), requestBody).Result;
        }

        private static HttpResponseMessage CreateApi(HttpClient client, string subscriptionId, string providerId, string apiName, string apiPath, ApiVersion version, List<ProductResponse> productResponses)
        {
            var openApiSpecContent = GetByteArrayContent(version.OpenApiSpecFile);
            var logoContent = GetByteArrayContent(version.ApiLogoFile);
            var docContent = GetByteArrayContent(version.ApiDocumentation);
            var productIds = productResponses.Where(x => version.ProductNames.Contains(x.Name)).Select(x => x.Id).ToList();

            var requestBody = new MultipartFormDataContent 
            {
                { new StringContent(apiName, Encoding.UTF8),"name"},
                { openApiSpecContent, "openApiSpec", version.OpenApiSpecFile.Substring(version.OpenApiSpecFile.LastIndexOf('/')+1) },
                { new StringContent(apiPath, Encoding.UTF8), "path" },
                { new StringContent(version.PathIdentifier, Encoding.UTF8), "apiVersion" },
                { new StringContent(providerId, Encoding.UTF8), "providerId" },
                { new StringContent(productIds[0].ToString(), Encoding.UTF8), "productIds" },
                { new StringContent("http://www.google.com/", Encoding.UTF8), "backendServiceUrl" }
            };

            return client.PostAsync(new Uri($"/subscriptions/{subscriptionId}/gateway/apis", UriKind.Relative), requestBody).Result;
        }

        private static HttpResponseMessage CreateApiVersion(HttpClient client, string subscriptionId, string providerId, string apiName, string apiPath, string apiVersionSetId, ApiVersion version, List<ProductResponse> productResponses)
        {
            var openApiSpecContent = GetByteArrayContent(version.OpenApiSpecFile);
            var logoContent = GetByteArrayContent(version.ApiLogoFile);
            var docContent = GetByteArrayContent(version.ApiDocumentation);
            var productIds = productResponses.Where(x => version.ProductNames.Contains(x.Name)).Select(x => x.Id).ToList();

            var requestBody = new MultipartFormDataContent
            {
                { new StringContent(apiName, Encoding.UTF8),"name"},
                { openApiSpecContent, "openApiSpec", version.OpenApiSpecFile.Substring(version.OpenApiSpecFile.LastIndexOf('/') + 1) },
                { new StringContent(apiPath, Encoding.UTF8), "path" },
                { new StringContent(version.PathIdentifier, Encoding.UTF8), "apiVersion" },
                { new StringContent(providerId, Encoding.UTF8), "providerId" },
                { new StringContent(productIds[0].ToString(), Encoding.UTF8), "productIds" },
                { new StringContent("http://www.google.com/", Encoding.UTF8), "backendServiceUrl" }
            };

            return client.PostAsync(new Uri($"/subscriptions/{subscriptionId}/gateway/apis?apiVersionSetId={apiVersionSetId}", UriKind.Relative), requestBody).Result;
        }

        private static HttpResponseMessage CreateApiRevision(HttpClient client, string subscriptionId, string apiId, Revision revision)
        {
            var openApiSpecContent = GetByteArrayContent(revision.OpenApiSpecFile);
            var revisionRequest = new MultipartFormDataContent
                {
                    { new StringContent(revision.RevisionDescription, Encoding.UTF8),"revisionDescription"},
                    { openApiSpecContent, "openApiSpec", revision.OpenApiSpecFile.Substring(revision.OpenApiSpecFile.LastIndexOf('/') + 1) },
                };

            return client.PostAsync(new Uri($"subscriptions/{subscriptionId}/gateway/apis/{apiId}/revisions", UriKind.Relative), revisionRequest).Result;
        }

        private static HttpResponseMessage DeleteProduct(HttpClient client, string subscriptionId, string productId)
        {
            return client.DeleteAsync(new Uri($"/subscriptions/{subscriptionId}/gateway/products/{productId}", UriKind.Relative)).Result;
        }

        private static HttpResponseMessage DeleteApi(HttpClient client, string subscriptionId, string apiId)
        {
            return client.DeleteAsync(new Uri($"/subscriptions/{subscriptionId}/gateway/apis/{apiId}", UriKind.Relative)).Result;
        }

        private static ByteArrayContent GetByteArrayContent(string filePath)
        {
            var fileBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), $"Publish/{filePath}"));
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            fileContent.Headers.ContentLength = fileBytes.Length;
            return fileContent;
        }

        private static string GetXML(string filePath)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), $"Publish/{filePath}"));
        }
    }
}
