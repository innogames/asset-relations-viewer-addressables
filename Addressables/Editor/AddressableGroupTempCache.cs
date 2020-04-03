using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup.Addressables
{
	public class AddressableGroupDependencyNode : IResolvedNode
	{
		public string sId;

		public string Id{get { return sId; }}
		public string Type { get { return "AddressableGroup"; }}
		public bool Existing{get { return true; }}
		
		public List<Dependency> Dependencies = new List<Dependency>();
	}
	
	public class AddressableGroupTempCache : IDependencyCache
	{
		private IResolvedNode[] Nodes = new IResolvedNode[0];
		private Dictionary<string, AddressableGroupDependencyNode> Lookup = new Dictionary<string, AddressableGroupDependencyNode>();
		public const string ConnectionType = "AddressableGroupUsage";
		
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

		public void Update(ProgressBase progress)
		{
			AddressableAssetSettings settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

			if (settings == null)
			{
				return;
			}
			
			Lookup.Clear();
			Nodes = new IResolvedNode[0];
			
			List<IResolvedNode> nodes = new List<IResolvedNode>();

			foreach (AddressableAssetGroup group in settings.groups)
			{
				AddressableGroupDependencyNode node = new AddressableGroupDependencyNode();
				node.sId = group.Name;

				int i = 0;
				
				foreach (AddressableAssetEntry addressableAssetEntry in group.entries)
				{
					List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
					addressableAssetEntry.GatherAllAssets(entries, true, true, false);
					
					foreach (AddressableAssetEntry assetEntry in entries)
					{
						string componentName = "GroupUsage " + i++;
						node.Dependencies.Add(new Dependency(assetEntry.guid, "AddressableGroupUsage", "Asset", new []{new PathSegment(componentName, PathSegmentType.Property), }));
					}
				}

				nodes.Add(node);
				Lookup.Add(node.Id, node);
			}

			Nodes = nodes.ToArray();
		}

		public IResolvedNode[] GetNodes()
		{
			return Nodes;
		}

		public string GetHandledNodeType()
		{
			return "AddressableGroup";
		}

		public List<Dependency> GetDependenciesForId(string id)
		{
			var resolverUsagesLookup = _createdDependencyCache.ResolverUsagesLookup;

			if (resolverUsagesLookup.ContainsKey(AddressablesGroupResolver.Id) &&
			    resolverUsagesLookup[AddressablesGroupResolver.Id].ConnectionTypes.Contains(ConnectionType))
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
			return typeof(IAddressableGroupResolver);
		}
	}
	
	public interface IAddressableGroupResolver : IDependencyResolver
	{
	}

	public class AddressablesGroupResolver : IAddressableGroupResolver
	{
		public const string Id = "AddressableGroupResolver";
		
		private string[] ConnectionTypes = {AddressableGroupTempCache.ConnectionType};
		private static ConnectionType DependencyType = new ConnectionType(new Color(0.85f, 0.65f, 0.55f), false, true);
		
		public string[] GetConnectionTypes()
		{
			return ConnectionTypes;
		}

		public string GetId()
		{
			return Id;
		}

		public ConnectionType GetDependencyTypeForId(string typeId)
		{
			return DependencyType;
		}
	}
}
