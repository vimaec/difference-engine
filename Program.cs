// MIT License 
// Copyright the Difference Engine Team AECTech 20222 Hackathon
//
// This program uses the VIM SDK (visit https://vimaec.com) to compute 
// delta from a list of VIM files in a folder.
// The folder is passed as a command line arguments
// For every two files compared a JSON file is created containing the list of deltas
// And a geometry file is output in OBJ form for each type of change.

using System.Diagnostics;
using Vim;
using Vim.DotNetUtilities;
using Vim.DotNetUtilities.JsonSerializer;
using Vim.G3d;
using Vim.Geometry;
using Vim.Math3d;

// The different types of changes computed
public enum ChangeType
{
    Unchanged,  // No detected change 
    Addition,   // Addition of a new element ID
    Deletion,   // Removal of an element (looking at element ID)
    Resized,    // Change in volumen of the bounding box 
    Moved,      // Movement of the center of the bounding box in world space
    Changed,    // A change to one of the parameters
};

// Expresses the difference between a node and the previous node. 
record ChangeRecord(
    string ChangeType,
    string Category,
    string FamilyName,
    string FamilyType,
    string PreviousFamilyType,
    int ElementId,
    float Volume,
    float VolumeChange,
    Vector3 Center,
    float DistanceMoved,
    int NodeIndex
);

public static class Program
{
    // The different change types represented as strings 
    public static readonly IReadOnlyList<string> ChangeTypes
        = Enum.GetNames(typeof(ChangeType));

    // The amount of volume change required to consider an object resized (calculated from bounding box)
    public const float VolumeTolerance = 1.0f / 12;

    // The distance required to be moved (calculated from bounding box centers) before an object is considered moved
    public const float PositionTolerance = 1.0f / 12;

    // Creates a change record for a node in the VIM file compared to the previous VIM file
    static ChangeRecord CreateChangeRecord(ChangeType changeType, VimSceneNode node, VimSceneNode? prevNode)
    {
        var box = node.TransformedBoundingBox();
        if (prevNode == null)
        {
            return new(changeType.ToString(), node.CategoryName, node.FamilyName, node.FamilyTypeName, "", node.ElementId,
                box.Volume, 0, box.Center, 0, node.Id);
        }

        var prevBox = prevNode.TransformedBoundingBox();
        return new(changeType.ToString(), node.CategoryName, node.FamilyName, node.FamilyTypeName, prevNode.FamilyTypeName,
            node.ElementId, box.Volume, box.Volume - prevBox.Volume, box.Center, prevBox.Center.Distance(box.Center),
            node.Id);
    }

    // Outputs a single OBJ file containing the geometry of all objects of a specific change Type 
    static void OutputObj(IEnumerable<ChangeRecord> changes, VimScene vim, ChangeType changeType, int prefix,
        string outputFolder)
    {
        var meshes = changes.Where(c => c.ChangeType == changeType.ToString())
            .Select(c => vim.VimNodes[c.NodeIndex].TransformedMesh());
        if (!meshes.Any()) return;
        meshes.Merge().ToG3d().WriteObj(Path.Combine(outputFolder, $"{changeType}_{prefix}.obj"));
    }

    // A simplistic computation to check if the family or family type changed. 
    // A more sophisticated algorithm would create a detailed comparison of parameters and common field values 
    static bool IsChanged(VimSceneNode node1, VimSceneNode node2)
        => node1.FamilyName != node2.FamilyName
           || node1.FamilyTypeName != node2.FamilyTypeName;

    /// Main entry point of the script 
    public static void Main(params string[] args)
    {
        // Start a stopwatch
        var sw = Stopwatch.StartNew();

        // The first parameter on the command line is the input folder 
        var inputFolder = args[0];
        
        // Get all of the VIM files in the input folder ordered by their name
        var vimFilePaths = Directory.GetFiles(inputFolder, "*.vim").OrderBy(f => f).ToList();
        
        // Output folder is created as a sub-folder of the input folder and is named 
        var outputFolder = Path.Combine(inputFolder, "output");
        Util.CreateAndClearDirectory(outputFolder);
        
        // We are going to loop through all VIM files and load them in memory one by one 
        // comparing with the previous one. 
        var prevVim = VimScene.LoadVim(vimFilePaths[0]);
        for (var i = 0; i < vimFilePaths.Count; ++i)
        {
            // When looking at the first file we have to treat it as a special case, so current and previous is the same 
            var currVim = i > 0 ? VimScene.LoadVim(vimFilePaths[i]) : prevVim;

            var prevNodes = prevVim.VimNodesWithGeometry().ToDictionaryIgnoreDuplicates(n => n.ElementId, n => n);
            var currNodes = currVim.VimNodesWithGeometry().ToDictionaryIgnoreDuplicates(n => n.ElementId, n => n);

            var changes = new List<ChangeRecord>();
            foreach (var currNode in currVim.VimNodesWithGeometry())
            {
                // All nodes in the first file are treated as additions 
                // In other files, 
                if (i == 0 || !prevNodes.ContainsKey(currNode.ElementId))
                {
                    changes.Add(CreateChangeRecord(ChangeType.Addition, currNode, null));
                }
                else
                {
                    // Look for the different possible types of change 
                    var prevNode = prevNodes[currNode.ElementId];

                    var prevBox = prevNode.TransformedBoundingBox();
                    var currBox = currNode.TransformedBoundingBox();

                    if (IsChanged(prevNode, currNode))
                    {
                        changes.Add(CreateChangeRecord(ChangeType.Changed, currNode, prevNode));
                    }
                    else if (!prevBox.Volume.AlmostEquals(currBox.Volume, VolumeTolerance))
                    {
                        changes.Add(CreateChangeRecord(ChangeType.Resized, currNode, prevNode));
                    }
                    else if (!prevBox.Center.AlmostEquals(currBox.Center, PositionTolerance))
                    {
                        changes.Add(CreateChangeRecord(ChangeType.Moved, currNode, prevNode));
                    }
                    else
                    {
                        changes.Add(CreateChangeRecord(ChangeType.Unchanged, currNode, prevNode));
                    }
                }
            }

            // To look for deleted nodes we have to loop through the previous nodes
            // We can skip this process if we are in a new node
            if (i != 0)
            {
                foreach (var node1 in prevVim.VimNodesWithGeometry())
                {
                    if (!currNodes.ContainsKey(node1.ElementId))
                    {
                        changes.Add(CreateChangeRecord(ChangeType.Deletion, node1, null));
                    }
                }
            }

            // Output a merged OBJ for each change type 
            OutputObj(changes, currVim, ChangeType.Changed, i, outputFolder);
            OutputObj(changes, currVim, ChangeType.Moved, i, outputFolder);
            OutputObj(changes, currVim, ChangeType.Resized, i, outputFolder);
            OutputObj(changes, currVim, ChangeType.Addition, i, outputFolder);
            OutputObj(changes, currVim, ChangeType.Unchanged, i, outputFolder);

            // Notice that Deletion records required the previous VIM 
            OutputObj(changes, prevVim, ChangeType.Deletion, i, outputFolder);

            // The delta file contains all of the change records 
            var deltaFile = Path.Combine(outputFolder, $"delta_{i}.json");
            changes.ToJsonFile(deltaFile);
            prevVim = currVim;
        }

        Util.OutputTimeElapsed(sw, "Computing all deltas");
    }
}