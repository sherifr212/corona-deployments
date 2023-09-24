using CoronaDeployments.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoronaDeployments
{
    public static class Utils
    {
        public static List<SelectListItem> FromEnum<T>() where T : Enum
        {
            var type = typeof(T);
            var result = new List<SelectListItem>();
            
            foreach (var item in Enum.GetValues(type))
            {
                result.Add(new SelectListItem(GetWordsFromCamelCase(Enum.GetName(type, item)), ((int)item).ToString()));
            }

            return result;
        }

        // https://stackoverflow.com/a/5796427/114434
        public static string GetWordsFromCamelCase(string val)
        {
            return Regex.Replace(val, "(\\B[A-Z])", " $1");
        }
    }

    public static class MvcAlertUtils
    {
        private const string SysMsg = "SystemMessage";
        private const string SysMsgClass = "SystemMessageClass";

        public static void AlertError(this Controller self, string message)
        {
            self.TempData[SysMsg] = message;
            self.TempData[SysMsgClass] = "alert alert-danger";
        }

        public static void AlertInfo(this Controller self, string message)
        {
            self.TempData[SysMsg] = message;
            self.TempData[SysMsgClass] = "alert alert-info";
        }

        public static void AlertSuccess(this Controller self, string message)
        {
            self.TempData[SysMsg] = message;
            self.TempData[SysMsgClass] = "alert alert-success";
        }
    }

    public static class SessionUtils
    {
        private const string Key = "_custom_session";

        public static async Task SetSession(this HttpContext self, Session session)
        {
            await self.Session.LoadAsync();

            var json = JsonConvert.SerializeObject(session);
            self.Session.SetString(Key, json);
        }

        public static async Task<Session> GetSession(this HttpContext self)
        {
            await self.Session.LoadAsync();

            var json = self.Session.GetString(Key);
            if (json != null)
            { 
                return JsonConvert.DeserializeObject<Session>(json);
            }

            return null;
        }
    }
}
