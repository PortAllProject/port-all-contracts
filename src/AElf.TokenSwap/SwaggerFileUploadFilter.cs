using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AElf.TokenSwap
{
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.HttpMethod != "PUT" && context.ApiDescription.HttpMethod != "POST")
            {
                return;
            }

            var parameters = context.ApiDescription.ActionDescriptor.Parameters;

            var formFileParams = (from parameter in parameters
                where parameter.ParameterType.IsAssignableFrom(typeof(IFormFile))
                select parameter).ToArray();

            if (formFileParams.Length < 1)
            {
                return;
            }

            if (!operation.RequestBody.Content.TryGetValue("multipart/form-data", out OpenApiMediaType content))
            {
                content = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema()
                };

                operation.RequestBody.Content["multipart/form-data"] = content;
            }

            content.Schema.Properties.Clear();
            content.Encoding.Clear();

            foreach (var formFileParam in formFileParams)
            {
                var argumentName = formFileParam.Name;
                operation.RequestBody.Content["multipart/form-data"].Schema.Properties.Add(argumentName,
                    new OpenApiSchema {Type = "string", Format = "binary"});
            }
        }
    }
}