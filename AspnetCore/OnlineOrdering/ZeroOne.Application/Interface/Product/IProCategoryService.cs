using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroOne.Extension;

namespace ZeroOne.Application
{
    public interface IProCategoryService
    {
        /// <summary>
        /// ��ȡ��Ʒ���������б�
        /// </summary>
        /// <returns></returns>
        Task<IList<SelectItem<string, Guid>>> GetSelectItems();
    }
}