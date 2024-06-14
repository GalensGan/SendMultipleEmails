﻿using LiteDB;
using Uamazing.Utils.Database.Attributes;
using Uamazing.Utils.Database.LiteDB;
using Uamazing.Utils.Extensions;
using Uamazing.Utils.Helpers;

namespace UZonMailService.Utils.Database
{
    /// <summary>
    /// 自定义的 BsonMapper
    /// </summary>
    public class SMEBsonMapper : BsonMapper
    {
        /// <summary>
        /// SME LiteDB 数据库映射
        /// </summary>
        public SMEBsonMapper()
        {
            ResolveCollectionName = ResolveCollectionNameFunc;
            UseCamelCase();
            EnumAsInteger = true;
        }

        /// <summary>
        /// 获取集合名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string ResolveCollectionNameFunc(Type type)
        {
            CollectionNameAttribute att = AttributeHelper.GetAttribute<CollectionNameAttribute>(type);
            if (att == null) return type.Name.ToSnakeCase();
            return att.Name.ToSnakeCase();
        }
    }
}
