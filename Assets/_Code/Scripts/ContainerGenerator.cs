using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ContainerGenerator
{
	public string Name { get => m_Name; }
	public int Size { get => m_Size; }

	[SerializeField]
	private string m_Name;

	[SerializeField]
	private ItemContainerFlags m_Flag;

	[SerializeField]
	[Range(1, 100)]
	private int m_Size = 1;

	[BHeader("Item Filtering")]

	[SerializeField]
	private bool m_OneStackPerItem;

	[SerializeField]
	private string[] m_ValidCategories;

	[SerializeField]
	private string[] m_RequiredProperties;


	public ItemContainer GenerateContainer(Transform parent)
	{
		var container = new ItemContainer(
			m_Name,
			m_Size,
			parent,
			m_Flag,
			m_OneStackPerItem,
			m_ValidCategories,
			m_RequiredProperties);

		return container;
	}
}