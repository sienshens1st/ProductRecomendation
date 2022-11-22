using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace egitlab_PotionNetCore.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)] // ini halaman masih buat test di /privacy doang, masih pake policy provider auth
    public class AuthorizeCustomAttribute : Attribute, IAuthorizationFilter
    {
        private IList<Role> _roles;
        public AuthorizeCustomAttribute(params Role[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            HttpContextAccessor contextAccessor = new HttpContextAccessor();
            IList<Role> roles = new List<Role>();

            // skip authorization if action is decorated with [AllowAnonymous] attribute
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous)
                return;

            // authorization
            var rolelistClaims = contextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            Role roleEnum;
            
            roleEnum = Enum.Parse<Role>(rolelistClaims);
            roles.Add(roleEnum);

            if ((_roles.Any() && roles.All(s => !_roles.Contains(s)))) // s itu isi dari role auth list
                context.Result = new ForbidResult();
        }
    }

    public enum Role
    {
        admin,
        salesman,
        customer
    }
}