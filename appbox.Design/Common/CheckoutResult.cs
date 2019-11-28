using appbox.Models;

namespace appbox.Design
{
    sealed class CheckoutResult
    {
        public bool Success { get; }

        /// <summary>
        /// 签出单个模型时，已被其他人修改(版本变更), 则返回当前最新的版本的模型
        /// </summary>
        public ModelBase ModelWithNewVersion { get; internal set; }

        public CheckoutResult(bool success)
        {
            Success = success;
        }

    }

}
