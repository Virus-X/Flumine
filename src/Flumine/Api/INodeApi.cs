using Flumine.Model;
using Flumine.Nancy.Model;

namespace Flumine.Api
{
    public interface INodeApi
    {
        NodeDescriptor GetState();

        void AssignShares(ShareAssignmentArgs shareAssignment);

        void ReleaseShares(ShareAssignmentArgs shareAssignment);
    }
}