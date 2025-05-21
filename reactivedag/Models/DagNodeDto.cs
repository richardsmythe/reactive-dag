namespace ReactiveDAG.Core.Engine
{
        public class DagNodeDto
        {
            public int Index { get; set; }
            public string Type { get; set; }
            public object Value { get; set; }
            public int[] Dependencies { get; set; }
        }
    
}