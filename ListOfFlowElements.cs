using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Redpoint.FlowGraph
{
    [CollectionDataContract]
    public class ListOfFlowElements : List<FlowElement>
    {
    }
}

