using System;
using System.Collections.Generic;
using System.Linq;
using Com.Innogames.Core.Frontend.AssetRelationsViewer;
using UnityEditor;
using UnityEngine;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup.Addressables
{
	public class AddressableGroupVisualizationNodeData : VisualizationNodeData
	{
		public override Texture2D AssetPreviewTexture
		{
			get { return null; }
		}

		public override Texture2D ThumbNailTexture
		{
			get { return null; }
		}
	}

	public class AddressableAssetGroupTypeHandler : ITypeHandler
	{
		private string[] m_nodes = new string[0];
		private string[] m_filteredNodes = new string[0];

		private string m_selectedGroupId = String.Empty;
		private string m_filter = String.Empty;

		private AddressableGroupNodeHandler _nodeHandler;

		private int m_dropDownIndex = 0;
		
		private AssetRelationsViewerWindow _viewerWindow;
		
		public string GetHandledType()
		{
			return AddressableAssetGroupNodeType.Name;
		}

		public string GetSortingKey(string name)
		{
			return name;
		}

		public VisualizationNodeData CreateNodeCachedData(string id)
		{
			return new AddressableGroupVisualizationNodeData();
		}

		public void SelectInEditor(string id)
		{
		}

		public void InitContext(NodeDependencyLookupContext context, AssetRelationsViewerWindow viewerWindow, INodeHandler nodeHandler)
		{
			_viewerWindow = viewerWindow;
			_nodeHandler = nodeHandler as AddressableGroupNodeHandler;
		
			HashSet<string> nodes = new HashSet<string>();
			
			foreach (KeyValuePair<string,CreatedDependencyCache> pair in context.CreatedCaches)
			{
				List<IDependencyMappingNode> resolvedNodes = new List<IDependencyMappingNode>();
				pair.Value.Cache.AddExistingNodes(resolvedNodes);

				foreach (IDependencyMappingNode node in resolvedNodes)
				{
					if(node.Type == AddressableAssetGroupNodeType.Name)
						nodes.Add(node.Id);
				}
			}

			m_nodes = nodes.ToArray();
			m_filteredNodes = nodes.ToArray();
		}

		public bool HandlesCurrentNode()
		{
			return !string.IsNullOrEmpty(m_selectedGroupId);
		}

		public void OnGui()
		{
			if (m_nodes.Length == 0)
			{
				EditorGUILayout.LabelField("AddressableGroupTempCache not activated");
				EditorGUILayout.LabelField("or no addressable groups found");
				return;
			}
			
			EditorGUILayout.LabelField("Selected Group:");
			EditorGUILayout.LabelField(m_selectedGroupId);
			EditorGUILayout.Space();
			
			string newFilter = EditorGUILayout.TextField("Filter:", m_filter);

			if (newFilter != m_filter)
			{
				m_filter = newFilter;
				HashSet<string> filteredNodes = new HashSet<string>();

				foreach (string node in m_nodes)
				{
					if (node.Contains(m_filter))
						filteredNodes.Add(node);
				}
				
				m_filteredNodes = filteredNodes.ToArray();
			}

			m_dropDownIndex = EditorGUILayout.Popup("Groups: ", m_dropDownIndex, m_filteredNodes);

			if (GUILayout.Button("Select"))
			{
				m_selectedGroupId = m_filteredNodes[m_dropDownIndex];
				_viewerWindow.ChangeSelection(m_selectedGroupId, AddressableAssetGroupNodeType.Name);
			}
		}

		public void OnSelectAsset(string id, string type)
		{
			if (type == AddressableAssetGroupNodeType.Name)
				m_selectedGroupId = id;
			else
				m_selectedGroupId = String.Empty;
		}
	}
	
	public class AddressableGroupNodeHandler : INodeHandler
	{
		public string GetId()
		{
			return "AddressableGroupNodeHandler";
		}

		public string GetHandledNodeType()
		{
			return AddressableAssetGroupNodeType.Name;
		}
	
		public int GetOwnFileSize(string type, string id, string key,
			NodeDependencyLookupContext stateContext,
			Dictionary<string, NodeDependencyLookupUtility.NodeSize> ownSizeCache)
		{
			Node node = stateContext.RelationsLookup.GetNode(key);
			HashSet<Node> addedNodes = new HashSet<Node>();
			HashSet<Node> addedFiles = new HashSet<Node>();
			
			GetTreeNodes(node, stateContext, addedNodes, addedFiles, 0);

			int size = 0;
			
			foreach (Node addedNode in addedFiles)
			{
				size += NodeDependencyLookupUtility.GetOwnNodeSize(addedNode.Id, addedNode.Type, addedNode.Key,
						stateContext, ownSizeCache);
			}
			
			return size;
		}

		private void GetTreeNodes(Node node, NodeDependencyLookupContext stateContext, HashSet<Node> addedNodes, HashSet<Node> addedFiles, int depth)
		{
			if (addedNodes.Contains(node))
			{
				return;
			}

			addedNodes.Add(node);

			if (depth > 1)
			{
				foreach (Connection referencerConnection in node.Referencers)
				{
					if (referencerConnection.Node.Type == AddressableAssetGroupNodeType.Name)
					{
						return;
					}
				}
			}

			if (node.Type == FileNodeType.Name)
			{
				addedFiles.Add(node);
				return;
			}
			
			foreach (Connection dependency in node.Dependencies)
			{
				if (!stateContext.DependencyTypeLookup.GetDependencyType(dependency.DependencyType).IsHard)
				{
					return;
				}
				
				string dependencyNodeType = dependency.Node.Type;

				if (dependencyNodeType == AssetNodeType.Name || dependencyNodeType == FileNodeType.Name)
				{
					GetTreeNodes(dependency.Node, stateContext, addedNodes, addedFiles, depth + 1);
				}
			}
		}

		public bool IsNodePackedToApp(string id, string type, bool alwaysExclude)
		{
			return true;
		}

		public bool IsSceneAndPacked(string path)
		{
			return false;
		}

		public bool IsInResources(string path)
		{
			return false;
		}

		public bool IsNodeEditorOnly(string id, string type)
		{
			return false;
		}

		public bool ContributesToTreeSize()
		{
			return false;
		}

		public void GetNameAndType(string id, out string name, out string type)
		{
			name = id;
			type = "AddressableAssetGroupBundle";
		}

		public long GetChangedTimeStamp(string id)
		{
			return -1;
		}

		public void InitContext(NodeDependencyLookupContext nodeDependencyLookupContext)
		{
		}
	}
}
