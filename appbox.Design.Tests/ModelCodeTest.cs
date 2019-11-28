using System;
using System.Collections.Generic;
using Xunit;
using appbox.Design;
using appbox.Runtime;
using appbox.Models;
using System.Threading.Tasks;
using System.Text;

namespace appbox.Design.Tests
{
    public class ModelCodeTest
    {
        /// <summary>
        /// 编解码测试
        /// </summary>
        [Fact]
        public void CodecTest()
        {
            string sourceCode = $"中国using System;\nusing System.Threading.Tasks;\n\nnamespace sys.ServiceLogic\n{{\n\tpublic class HelloService\n\t{{\n\t}}\n}}BB";
            string declareCode = $"人民using System;\nusing System.Threading.Tasks;\n\nnamespace sys.ServiceLogic\n{{\n\tpublic class HelloService\n\t{{\n\t}}\n}}DD";

            var utf8Data = Encoding.UTF8.GetBytes(sourceCode);
            Console.WriteLine(utf8Data.Length * 2);

            byte[] data = Store.ModelCodeUtil.EncodeServiceCode(sourceCode, declareCode);
            Console.WriteLine(data.Length);

            string code1;
            string code2;
            Store.ModelCodeUtil.DecodeServiceCode(data, out code1, out code2);
            Assert.Equal(sourceCode, code1);
            Assert.Equal(declareCode, code2);
        }
    }
}
