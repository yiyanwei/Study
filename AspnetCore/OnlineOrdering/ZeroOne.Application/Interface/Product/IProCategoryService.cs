using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroOne.Entity;
using ZeroOne.Extension.Model;

namespace ZeroOne.Application
{
    public interface IProCategoryService : IBaseService<ProCategory, Guid>
    {
        /// <summary>
        /// ��ȡ��Ʒ���������б�
        /// </summary>
        /// <returns></returns>
        Task<IList<SelectItem<string, Guid>>> GetSelectItems();

        /// <summary>
        /// ��Ӳ�Ʒ����
        /// </summary>
        /// <param name="request">��Ʒ�����������</param>
        /// <returns></returns>
        Task<ProCategory> AddEntityAsync(ProCategoryAddRequest request);

        /// <summary>
        /// ���²�Ʒ����
        /// </summary>
        /// <param name="request">�������</param>
        /// <returns></returns>
        Task<bool> UpdateEntityAsync(ProCategoryEditRequest request);
    }
}