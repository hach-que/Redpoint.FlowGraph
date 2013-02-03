//
// This source code is licensed in accordance with the licensing outlined
// on the main Tychaia website (www.tychaia.com).  Changes to the
// license on the website apply retroactively.
//
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

