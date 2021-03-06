using Microsoft.Extensions.DependencyInjection;
using ZeroOne.Repository;

namespace ZeroOne.Application
{
    /// <summary>
    /// 注册服务
    /// </summary>
    public static class ServiceRegister
    {
        /// <summary>
        /// 注册仓储服务
        /// </summary>
        /// <param name="services">服务容器对象</param>
        /// <returns>服务容器对象</returns>
        public static IServiceCollection AddRepository(this IServiceCollection services)
        {
            //产品分类仓储服务
            services.AddTransient<IProCategoryRep, ProCategoryRep>();
            //产品详情仓储服务
            services.AddTransient<IProInfoRep,ProInfoRep>();
            //用户仓储服务
            services.AddTransient<IUserInfoRep, UserInfoRep>();
            //文件仓储服务
            services.AddTransient<IFileInfoRep, FileInfoRep>();
            //供应商仓储服务
            services.AddTransient<ISupplierRep, SupplierRep>();
            //添加区域仓储服务
            services.AddTransient<IDistrictRep, DistrictRep>();
            return services;
        }
    }
}