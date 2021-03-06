﻿using System;
using System.Net;
using System.ServiceModel.Web;
using Insight.Utils.Client;
using Insight.Utils.Entity;

namespace Insight.Utils.Server
{
    public class Verify
    {
        /// <summary>
        /// 验证结果
        /// </summary>
        public Result Result = new Result();

        /// <summary>
        /// Access Token
        /// </summary>
        public string Token;

        /// <summary>
        /// 通过Access Token校验是否有权限访问
        /// </summary>
        /// <param name="verifyurl">验证服务URL</param>
        /// <param name="limit">限制调用时间间隔（秒），默认不启用</param>
        /// <param name="anonymous">是否允许匿名访问（默认不允许）</param>
        public Verify(string verifyurl, int limit = 0, bool anonymous = false)
        {
            if (anonymous)
            {
                if (!GetToken())
                {
                    Result.InvalidAuth();
                    return;
                }

                Result = new HttpClient(verifyurl).Request(Token);
                if (Result.Successful) return;

                var time = CallManage.LimitCall(limit <= 0 ? 60 : limit);
                if (time > 0)
                {
                    Result.TooFrequent(time);
                    return;
                }

                Result.Success();
            }
            else
            {
                var time = CallManage.LimitCall(limit);
                if (time > 0)
                {
                    Result.TooFrequent(time);
                    return;
                }

                if (!GetToken())
                {
                    Result.InvalidAuth();
                    return;
                }

                Result = new HttpClient(verifyurl).Request(Token);
            }
        }

        /// <summary>
        /// 带鉴权的会话合法性验证
        /// </summary>
        /// <param name="verifyurl">验证服务URL</param>
        /// <param name="aid">操作ID</param>
        /// <param name="limit">限制调用时间间隔（秒），默认不启用</param>
        public Verify(string verifyurl, Guid aid, int limit = 0)
        {
            var time = CallManage.LimitCall(limit);
            if (time > 0)
            {
                Result.TooFrequent(time);
                return;
            }

            if (!GetToken())
            {
                Result.InvalidAuth();
                return;
            }

            var url =  $"{verifyurl}/auth?action={aid}";
            Result = new HttpClient(url).Request(Token);
        }

        /// <summary>
        /// 获取Http请求头部承载的Access Token
        /// </summary>
        /// <returns>boll Http请求头部是否承载Access Token</returns>
        private bool GetToken()
        {
            var context = WebOperationContext.Current;
            if (context == null) return false;

            var headers = context.IncomingRequest.Headers;
            var response = context.OutgoingResponse;
            Token = headers[HttpRequestHeader.Authorization];
            if (!string.IsNullOrEmpty(Token)) return true;

            response.StatusCode = HttpStatusCode.Unauthorized;
            return false;
        }
    }
}
