﻿using System;
using System.Collections.Generic;
using System.Text;
using Insight.Utils.Common;
using Insight.Utils.Entity;

namespace Insight.Utils.Client
{

    public class TokenHelper
    {
        private string _Token;
        private string _RefreshToken;
        private DateTime _ExpiryTime;
        private DateTime _FailureTime;

        /// <summary>
        /// AccessToken字符串
        /// </summary>
        public string AccessToken => GetToken();

        /// <summary>
        /// AccessToken对象
        /// </summary>
        public AccessToken Token { get; private set; }

        /// <summary>
        /// 用户签名
        /// </summary>
        public string Sign { get; private set; }

        /// <summary>
        /// 当前连接基础应用服务器
        /// </summary>
        public string BaseServer { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 当前登录部门ID
        /// </summary>
        public Guid? DeptId { get; set; }

        /// <summary>
        /// 生成签名
        /// </summary>
        /// <param name="secret">用户密钥</param>
        public void Signature(string secret)
        {
            Sign = Util.Hash(Account.ToUpper() + Util.Hash(secret));
        }

        /// <summary>
        /// 获取AccessToken
        /// </summary>
        /// <returns>bool 是否获取成功</returns>
        public bool GetTokens()
        {
            var dict = GetCode();
            if (dict == null) return false;

            var key = Util.Hash(Sign + dict["Stamp"]);
            var url = $"{BaseServer}/security/v1.0/tokens?id={dict["ID"]}&account={Account}&signature={key}&deptid={DeptId}";
            var result = new HttpClient(url).Request();
            if (!result.Successful)
            {
                Messages.ShowError(result.Message);
                return false;
            }

            var data = Util.Deserialize<TokenResult>(result.Data);
            _Token = data.AccessToken;
            _RefreshToken = data.RefreshToken;
            _ExpiryTime = data.ExpiryTime;
            _FailureTime = data.FailureTime;

            var buffer = Convert.FromBase64String(_Token);
            var json = Encoding.UTF8.GetString(buffer);
            Token = Util.Deserialize<AccessToken>(json);
            return true;
        }

        /// <summary>
        /// 获取Code
        /// </summary>
        /// <returns>Dictionary Code</returns>
        private Dictionary<string, string> GetCode()
        {
            var url = $"{BaseServer}/security/v1.0/codes?account={Account}";
            var result = new HttpClient(url).Request();
            if (result.Successful) return Util.Deserialize<Dictionary<string, string>>(result.Data);

            const string str = "配置错误！请检查配置文件中的BaseServer项是否配置正确。";
            var msg = result.Code == "400" ? str : result.Message;
            Messages.ShowError(msg);
            return null;
        }

        /// <summary>
        /// 刷新AccessToken过期时间
        /// </summary>
        private void RefresTokens()
        {
            var url = $"{BaseServer}/security/v1.0/tokens";
            var result = new HttpClient(url, "PUT").Request(_RefreshToken);
            if (result.Code == "406")
            {
                GetTokens();
                return;
            }

            if (!result.Successful)
            {
                Messages.ShowError(result.Message);
                return;
            }

            var data = Util.Deserialize<TokenResult>(result.Data);
            _ExpiryTime = data.ExpiryTime;
        }

        /// <summary>
        /// 检查当前Token并返回
        /// </summary>
        /// <returns>string AccessToken</returns>
        private string GetToken()
        {
            var now = DateTime.Now;
            if (string.IsNullOrEmpty(_Token) || now > _FailureTime) GetTokens();

            if (now > _ExpiryTime) RefresTokens();

            return _Token;
        }
    }
}
