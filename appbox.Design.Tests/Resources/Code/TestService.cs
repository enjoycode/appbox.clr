using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sys.ServiceLogic
{
    public class TestService
    {
        public Task Test (string name)
        {
            throw new NotImplementedException();
        }

        public Task<string> Test1(string name)
        {
            throw new NotImplementedException();
        }

        public Task<int> Test2(int year)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime> Test3(DateTime year)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Entities.OrgUnit>> Test4()
		{
            throw new NotImplementedException();
		}
    }
}
