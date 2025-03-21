﻿#region Apache License Version 2.0
/*----------------------------------------------------------------

Copyright 2025 Jeffrey Su & Suzhou Senparc Network Technology Co.,Ltd.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
except in compliance with the License. You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the
License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
either express or implied. See the License for the specific language governing permissions
and limitations under the License.

Detail: https://github.com/JeffreySu/WeiXinMPSDK/blob/master/license.md

----------------------------------------------------------------*/
#endregion Apache License Version 2.0


/*----------------------------------------------------------------
    Copyright (C) 2025 Senparc

    文件名：SessionContainer.cs
    文件功能描述：小程序 Session 容器


    创建标识：Senparc - 20171008

    修改标识：Senparc - 20180614
    修改描述：CO2NET v0.1.0 ContainerBag 取消属性变动通知机制，使用手动更新缓存
  
    修改标识：Senparc - 20180701
    修改描述：V2.0.3 SessionBag 添加 UnionId 属性

    修改标识：Senparc - 20170522
    修改描述：v3.3.2 修改 DateTime 为 DateTimeOffset
    
    修改标识：Senparc - 20190422
    修改描述：v3.4.0 
             1、支持异步 Container
             2、SessionBag 默认有效期由 2 天调整为 5 天，并提供外部设置参数

    修改标识：Senparc - 20190712
    修改描述：v3.5.0 SessionContainer 添加 AddDecodedUserInfo() 方法，SessionBag 提供 DecodedUserInfo 属性
    
----------------------------------------------------------------*/


using Senparc.Weixin.Containers;
using Senparc.Weixin.WxOpen.Entities;
using Senparc.Weixin.WxOpen.Helpers;
using System;
using System.Threading.Tasks;

namespace Senparc.Weixin.WxOpen.Containers
{
    /// <summary>
    /// 第三方APP信息包
    /// </summary>
    [Serializable]
    public class SessionBag : BaseContainerBag
    {
        /// <summary>
        /// Session的Key（3rd_session / sessionId）
        /// </summary>
        public new string Key { get; set; }
        /// <summary>
        /// OpenId
        /// </summary>
        public string OpenId { get; set; }
        public string UnionId { get; set; }

        /// <summary>
        /// SessionKey
        /// </summary>
        public string SessionKey { get; set; }
        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTimeOffset ExpireTime { get; set; }

        //private string _key;
        //private string _openId;
        //private string _sessionKey;
        //private DateTimeOffset _expireTime;

        public DecodedUserInfo DecodedUserInfo { get; set; }

        /// <summary>
        /// ComponentBag
        /// </summary>
        public SessionBag()
        {
        }
    }


    /// <summary>
    /// 3rdSession容器
    /// </summary>
    public class SessionContainer : BaseContainer<SessionBag>
    {
        /// <summary>
        /// 获取最新的过期时间
        /// </summary>
        /// <returns></returns>
        private static TimeSpan GetExpireTime()
        {
            return TimeSpan.FromDays(5);//有效期5天
        }

        #region 同步方法

        /// <summary>
        /// 获取Session
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static SessionBag GetSession(string key)
        {
            var bag = TryGetItem(key);
            if (bag == null)
            {
                return null;
            }

            if (bag.ExpireTime < SystemTime.Now)
            {
                //已经过期
                Cache.RemoveFromCache(key);
                return null;
            }

            //using (FlushCache.CreateInstance())
            //{
            bag.ExpireTime = SystemTime.Now.Add(GetExpireTime());//滚动过期时间
            Update(key, bag, GetExpireTime());
            //}
            return bag;
        }

        /// <summary>
        /// 更新或插入SessionBag
        /// </summary>
        /// <param name="key">如果留空，则新建一条记录</param>
        /// <param name="openId">OpenId</param>
        /// <param name="sessionKey">SessionKey</param>
        /// <param name="uniondId">UnionId</param>
        /// <returns></returns>
        public static SessionBag UpdateSession(string key, string openId, string sessionKey, string uniondId, TimeSpan? expireTime = null)
        {
            key = key ?? SessionHelper.GetNewThirdSessionName();

            //using (FlushCache.CreateInstance())
            //{
            var sessionBag = new SessionBag()
            {
                Key = key,
                OpenId = openId,
                UnionId = uniondId,
                SessionKey = sessionKey,
                ExpireTime = SystemTime.Now.Add(expireTime ?? GetExpireTime())
            };
            Update(key, sessionBag, expireTime ?? GetExpireTime());
            return sessionBag;
            //}
        }

        /// <summary>
        /// 添加解码后的用户信息
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="decodedUserInfo"></param>
        public static void AddDecodedUserInfo(SessionBag bag, DecodedUserInfo decodedUserInfo)
        {
            bag.DecodedUserInfo = decodedUserInfo;
            Update(bag.Key, bag, bag.ExpireTime - SystemTime.Now);
        }

        #endregion

        #region 异步方法

        /// <summary>
        /// 获取Session
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static async Task<SessionBag> GetSessionAsync(string key)
        {
            var bag = await TryGetItemAsync(key).ConfigureAwait(false);
            if (bag == null)
            {
                return null;
            }

            if (bag.ExpireTime < SystemTime.Now)
            {
                //已经过期
                await Cache.RemoveFromCacheAsync(key).ConfigureAwait(false);
                return null;
            }

            //using (FlushCache.CreateInstance())
            //{
            bag.ExpireTime = SystemTime.Now.Add(GetExpireTime());//滚动过期时间
            await UpdateAsync(key, bag, GetExpireTime()).ConfigureAwait(false);
            //}
            return bag;
        }

        /// <summary>
        /// 更新或插入SessionBag
        /// </summary>
        /// <param name="key">如果留空，则新建一条记录</param>
        /// <param name="openId">OpenId</param>
        /// <param name="sessionKey">SessionKey</param>
        /// <param name="uniondId">UnionId</param>
        /// <returns></returns>
        public static async Task<SessionBag> UpdateSessionAsync(string key, string openId, string sessionKey, string uniondId, TimeSpan? expireTime = null)
        {
            key = key ?? SessionHelper.GetNewThirdSessionName();

            //using (FlushCache.CreateInstance())
            //{
            var sessionBag = new SessionBag()
            {
                Key = key,
                OpenId = openId,
                UnionId = uniondId,
                SessionKey = sessionKey,
                ExpireTime = SystemTime.Now.Add(expireTime ?? GetExpireTime())
            };
            await UpdateAsync(key, sessionBag, expireTime ?? GetExpireTime()).ConfigureAwait(false);
            return sessionBag;
            //}
        }

        /// <summary>
        /// 【异步方法】添加解码后的用户信息
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="decodedUserInfo"></param>
        public static async Task AddDecodedUserInfoAsync(SessionBag bag, DecodedUserInfo decodedUserInfo)
        {
            bag.DecodedUserInfo = decodedUserInfo;
            await UpdateAsync(bag.Key, bag, bag.ExpireTime - SystemTime.Now).ConfigureAwait(false);
        }


        #endregion
    }
}
