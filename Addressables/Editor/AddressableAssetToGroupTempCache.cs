using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup.Addressables
{
	public class AssetToAddressableGroupDependency
	{
		public const string Name = "AssetToAddressableGroup";
	}

	public class AddressableAssetToGroupTempCache : IDependencyCache
	{
		private IDependencyMappingNode[] Nodes = new IDependencyMappingNode[0];
		private Dictionary<string, GenericDependencyMappingNode> Lookup = new Dictionary<string, GenericDependencyMappingNode>();
		private CreatedDependencyCache _createdDependencyCache;
		
		public void ClearFile(string directory)
		{
		}

		public void Initialize(CreatedDependencyCache createdDependencyCache)
		{
			_createdDependencyCache = createdDependencyCache;
		}

		public bool NeedsUpdate()
		{
			return true;
		}

		public bool CanUpdate()
		{
			return !Application.isPlaying;
		}
		
		private RelationLookup.RelationsLookup GetAssetToFileLookup()
		{
			NodeDependencyLookupContext context = new NodeDependencyLookupContext();
			ResolverUsageDefinitionList resolverList = new ResolverUsageDefinitionList();
			resolverList.Add<AssetToFileDependencyCache, AssetToFileDependencyResolver>(true, true, true);
			NodeDependencyLookupUtility.LoadDependencyLookupForCaches(context, resolverList);

			return context.RelationsLookup;
		}

		public void Update()
		{
			RelationLookup.RelationsLookup assetToFileLookup = GetAssetToFileLookup();
			AddressableAssetSettings settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

			if (settings == null)
			{
				return;
			}
			
			Lookup.Clear();
			Nodes = new IDependencyMappingNode[0];
			
			List<IDependencyMappingNode> nodes = new List<IDependencyMappingNode>();

			for (int i = 0; i < settings.groups.Count; ++i)
			{
				AddressableAssetGroup group = settings.groups[i];
				EditorUtility.DisplayProgressBar("AddressableAssetToGroupTempCache", $"Getting dependencies for {group.Name}", i / (float)(settings.groups.Count));

				foreach (AddressableAssetEntry addressableAssetEntry in group.entries)
				{
					List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
					addressableAssetEntry.GatherAllAssets(entries, true, true, false);
					
					foreach (AddressableAssetEntry assetEntry in entries)
					{
						Node fileNode = assetToFileLookup.GetNode(assetEntry.guid, FileNodeType.Name);
						string assetId = fileNode.Referencers[0].Node.Id;
						GenericDependencyMappingNode node = new GenericDependencyMappingNode();

						node.NodeType = AssetNodeType.Name;
						node.NodeId = assetId;
						node.Dependencies.Add(new Dependency(group.Name, "GroupUsedByAsset", AddressableAssetGroupNodeType.Name, new []{new PathSegment("AssetGroup", PathSegmentType.Property), }));
						
						nodes.Add(node);
						Lookup.Add(node.Id, node);
					}
				}
			}

			Nodes = nodes.ToArray();
		}

		public void AddExistingNodes(List<IDependencyMappingNode> nodes)
		{
			foreach (IDependencyMappingNode node in Nodes)
			{
				if (node.Existing)
				{
					nodes.Add(node);
				}
			}
		}

		public string GetHandledNodeType()
		{
			return "Asset";
		}

		public List<Dependency> GetDependenciesForId(string id)
		{
			if(NodeDependencyLookupUtility.IsResolverActive(_createdDependencyCache, AddressableAssetToGroupResolver.Id, AssetToAddressableGroupDependency.Name) && Lookup.ContainsKey(id))	
			{
				return Lookup[id].Dependencies;
			}
			
			return new List<Dependency>();
		}

		public void Load(string directory)
		{
		}

		public void Save(string directory)
		{
		}

		public void InitLookup()
		{
		}

		public Type GetResolverType()
		{
			return typeof(IAddressableAssetToGroupResolver);
		}
	}
	
	public interface IAddressableAssetToGroupResolver : IDependencyResolver
	{
	}

	public class AddressableAssetToGroupResolver : IAddressableAssetToGroupResolver
	{
		public const string Id = "AddressableAssetToGroupResolver";
		
		private string[] ConnectionTypes = {AssetToAddressableGroupDependency.Name};
		private const string ConnectionTypeDescription = "Dependencies from the asset to the AddressableAssetGroup the asset is part of";
		private static DependencyType DependencyType = new DependencyType("Asset->AddressableAssetGroup", new Color(0.85f, 0.55f, 0.35f), false, true, ConnectionTypeDescription);
		
		public string[] GetDependencyTypes()
		{
			return ConnectionTypes;
		}

		public string GetId()
		{
			return Id;
		}

		public DependencyType GetDependencyTypeForId(string typeId)
		{
			return DependencyType;
		}
	}
}