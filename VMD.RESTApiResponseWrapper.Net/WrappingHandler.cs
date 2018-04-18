﻿using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using VMD.RESTApiResponseWrapper.Net.Wrappers;
using VMD.RESTApiResponseWrapper.Net.Extensions;
using VMD.RESTApiResponseWrapper.Net.Enums;

namespace VMD.RESTApiResponseWrapper.Net
{
    public class WrappingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            return BuildApiResponse(request, response);
        }

        private static HttpResponseMessage BuildApiResponse(HttpRequestMessage request, HttpResponseMessage response)
        {
            object content = null;
            object data = null;
            string errorMessage = null;
            ApiError apiError = null;
            
            var code = (int)response.StatusCode;

            if (response.TryGetContentValue(out content) && !response.IsSuccessStatusCode)
            {
                HttpError error = content as HttpError;

                //handle exception
                if (error != null)
                {
                    content = null;

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        apiError = new ApiError("The specified URI does not exist. Please verify and try again.");
                    else if (response.StatusCode == HttpStatusCode.NoContent)
                        apiError = new ApiError("The specified URI does not contain any content.");
                    else
                    {
                        errorMessage = error.Message;

                        #if DEBUG
                            errorMessage = string.Concat(errorMessage, error.ExceptionMessage, error.StackTrace);
                        #endif

                        apiError = new ApiError(errorMessage);
                    }
        
                    data = new APIResponse((int)code,ResponseMessageEnum.Failure.GetDescription(), null, apiError);

                }
                else
                    data = content;
            }
            else
                data = new APIResponse(code, ResponseMessageEnum.Success.GetDescription(), content);

            var newResponse = request.CreateResponse(response.StatusCode, data);        

            foreach (var header in response.Headers)
            {
                newResponse.Headers.Add(header.Key, header.Value);
            }

            return newResponse;
        }
    }
}
