﻿using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroOne.Entity;
using ZeroOne.Extension;
using ZeroOne.Repository;

namespace ZeroOne.Application
{
    public abstract class BaseService<TEntity, TPrimaryKey, TSearch> : BaseService<TEntity, TPrimaryKey>, IBaseService<TEntity, TPrimaryKey, TSearch>
        where TEntity : class, IEntity<TPrimaryKey>, new()
        where TSearch : BaseSearch
    {
        /// <summary>
        /// 获取查询过滤数据库对象
        /// </summary>
        /// <param name="search">查询对象</param>
        /// <returns></returns>
        protected abstract IList<BaseRepModel> GetBaseRepBySearch(TSearch search);

        private new IBaseRep<TEntity, TPrimaryKey, TSearch> Rep;
        public BaseService(IBaseRep<TEntity, TPrimaryKey, TSearch> rep, IMapper mapper) : base(rep, mapper)
        {
            this.Rep = rep;
        }

        public async Task<IList<TEntity>> GetEntitiesAsync(TSearch search)
        {
            var baseReps = this.GetBaseRepBySearch(search);
            if (baseReps?.Count > 0)
            {
                return await this.Rep.GetEntityListAsync(baseReps, search);
            }
            return new List<TEntity>();
        }

        //public async Task<TSearchResult> SearchResultAsync<TResult, TSearchResult>(TSearch search)
        //      where TResult : IResult, new()
        //    where TSearchResult : BaseSearchResult<TResult>, new()
        //{
        //    return await this.Rep.SearchResultAsync<TResult, TSearchResult>(search);
        //}


    }

    public abstract class BaseService<TEntity, TPrimaryKey> : IBaseService<TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>, new()
    {
        public virtual TResult FormatResult<TResult>(TResult result) where TResult : class, IResult, new()
        {
            return result;
        }

        public virtual TResult FormatResult<TResult>(TEntity entity) where TResult : class, IResult, new()
        {
            var result = Mapper.Map<TResult>(entity);
            return result;
        }

        protected IMapper Mapper { get; set; }

        protected IBaseRep<TEntity, TPrimaryKey> Rep;
        public BaseService(IBaseRep<TEntity, TPrimaryKey> rep, IMapper mapper)
        {
            this.Rep = rep;
            this.Mapper = mapper;
        }

        public async Task<TEntity> AddAndReturnAsync<TRequest>(TRequest request) where TRequest : IAddRequest
        {
            var entity = Mapper.Map<TEntity>(request);
            return await this.Rep.AddEntityAsync(entity);
        }

        public async Task<TEntity> AddAndReturnAsync(TEntity entity)
        {
            return await this.Rep.AddEntityAsync(entity);
        }

        /// <summary>
        /// 添加对象集合
        /// </summary>
        /// <param name="list">list集合</param>
        /// <returns></returns>
        public async Task<int> AddEntityListAsync(List<TEntity> list)
        {
            return await this.Rep.AddEntityListAsync(list);
        }


        public async Task<bool> DeleteAsync(TPrimaryKey id, Guid rowVersion, Guid? userId)
        {
            return await this.Rep.DeleteByIdAsync(id, rowVersion, userId);
        }

        public async Task<TResult> GetResultByIdAsync<TResult>(TPrimaryKey id) where TResult : class, IResult, new()
        {
            var entity = await this.Rep.GetEntityByIdAsync(id);
            return Mapper.Map<TResult>(entity);
        }

        public async Task<TEntity> GetEntityByIdAsync(TPrimaryKey id)
        {
            return await this.Rep.GetEntityByIdAsync(id);
        }

        public async Task<bool> UpdateNotNullAsync<TRequest>(TRequest request) where TRequest : IEditRequest
        {
            var entity = request.Map<TEntity>();
            return await this.Rep.UpdateEntityNotNullAsync(entity);
        }

        /// <summary>
        /// 获取最终分页结果
        /// </summary>
        /// <typeparam name="TPageSearch">分页查询类型参数</typeparam>
        /// <typeparam name="TResult">集合里的成员对象类型</typeparam>
        /// <typeparam name="TPageSearchResult">包括总页数以及分页的结果对象集合</typeparam>
        /// <param name="pageSearch">查询对象</param>
        public async Task<TPageSearchResult> SearchPageResultAsync<TPageSearch, TResult, TPageSearchResult>(TPageSearch pageSearch)
            where TPageSearch : BaseSearch, IPageSearch
            where TResult : IResult, new()
            where TPageSearchResult : PageSearchResult<TResult>, new()
        {
            return await this.Rep.SearchPageResultAsync<TPageSearch, TResult, TPageSearchResult>(pageSearch);
        }


        /// <summary>
        /// 获取最终分页结果
        /// </summary>
        /// <typeparam name="TPageSearch">分页查询类型参数</typeparam>
        /// <typeparam name="TResult">集合里的成员对象类型</typeparam>
        /// <typeparam name="TPageSearchResult">包括总页数以及分页的结果对象集合</typeparam>
        /// <param name="pageSearch">查询对象</param>
        public async Task<PageSearchResult<TResult>> SearchPageResultAsync<TPageSearch, TResult>(TPageSearch pageSearch)
            where TPageSearch : BaseSearch, IPageSearch
            where TResult : IResult, new()
        {
            return await this.Rep.SearchPageResultAsync<TPageSearch, TResult, PageSearchResult<TResult>>(pageSearch);
        }
    }
}
