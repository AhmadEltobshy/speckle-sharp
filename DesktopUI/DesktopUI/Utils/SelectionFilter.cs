﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using Newtonsoft.Json;
using Speckle.DesktopUI.Streams.Dialogs.FilterViews;
using Stylet;

namespace Speckle.DesktopUI.Utils
{

  public interface ISelectionFilter
  {
    string Name { get; set; }

    string Icon { get; set; }

    /// <summary>
    /// Used as the discriminator for deserialisation.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Shoud return a succint summary of the filter: what does it contain inside?
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Should contain a generic description of the filter and how it works.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Holds the values that the user selected from the filter. Not the actual objects.
    /// </summary>    
    List<string> Selection { get; set; }
  }

  public class ListSelectionFilter : ISelectionFilter
  {
    public string Type => typeof(ListSelectionFilter).ToString();

    public string Name { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }

    public List<string> Values { get; set; }
    public List<string> Selection { get; set; } = new List<string>();

    public string Summary
    {
      get
      {
        if (Selection.Count != 0)
        {
          return string.Join(", ", Selection);
        }
        else
        {
          return "Not set.";
        }
      }
    }
  }

  public class PropertySelectionFilter : ISelectionFilter
  {
    public string Type => typeof(PropertySelectionFilter).ToString();

    public string Name { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }

    public List<string> Selection { get; set; } = new List<string>();

    public List<string> Values { get; set; }
    public List<string> Operators { get; set; }
    public string PropertyName { get; set; }
    public string PropertyValue { get; set; }
    public string PropertyOperator { get; set; }
    public bool HasCustomProperty { get; set; }

    public string Summary
    {
      get
      {
        return $"{PropertyName} {PropertyOperator} {PropertyValue}";
      }
    }
  }

  public class FilterTab : PropertyChangedBase
  {
    public string Name => Filter.Name;

    public ISelectionFilter Filter { get; }

    public object FilterView { get; private set; }

    private BindableCollection<string> _listItems = new BindableCollection<string>();

    public BindableCollection<string> ListItems
    {
      get => _listItems;
      set => SetAndNotify(ref _listItems, value);
    }

    public FilterTab(ISelectionFilter filter)
    {
      Filter = filter;

      switch (filter)
      {
        case PropertySelectionFilter f:
          FilterView = new ParameterFilterView();
          break;
        case ListSelectionFilter f:
          FilterView = new ListFilterView();
          _valuesList = SearchResults = new BindableCollection<string>(f.Values);
          break;
      }
    }

    private string _searchQuery;
    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        SetAndNotify(ref _searchQuery, value);
        searchSourceChanged = true;
        SearchResults = new BindableCollection<string>(_valuesList.Where(v => v.ToLower().Contains(SearchQuery.ToLower())).ToList());
        NotifyOfPropertyChange(nameof(SearchResults));
      }
    }

    public BindableCollection<string> SearchResults { get; set; } = new BindableCollection<string>();
    public bool searchSourceChanged { get; set; } = false;
    private BindableCollection<string> _valuesList { get; }

    public void RemoveListItem(string name)
    {
      ListItems.Remove(name);
      if (SearchQuery != null && !name.Contains(SearchQuery)) return;
      SearchResults.Add(name);
    }
  }

  public class SelectionFilterConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(ISelectionFilter);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      var filter = serializer.Deserialize<ListSelectionFilter>(reader) ?? (ISelectionFilter)serializer.Deserialize<PropertySelectionFilter>(reader);

      return filter;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      serializer.Serialize(writer, value);
    }
  }

  // adapted from: https://tyrrrz.me/blog/wpf-listbox-selecteditems-twoway-binding
  public class MyObjectListBoxSelectionBehavior : ListBoxSelectionBehavior
  {
  }
  public class ListBoxSelectionBehavior : Behavior<ListBox>
  {
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register(nameof(SelectedItems), typeof(IList),
            typeof(ListBoxSelectionBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemsChanged));

    private static void OnSelectedItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
      var behavior = (ListBoxSelectionBehavior)sender;
      if (behavior._modelHandled) return;

      if (behavior.AssociatedObject == null)
        return;

      behavior._modelHandled = true;
      behavior.SelectItems();
      behavior._modelHandled = false;
    }

    private bool _viewHandled;
    private bool _modelHandled;

    public BindableCollection<string> SelectedItems
    {
      get => (BindableCollection<string>)GetValue(SelectedItemsProperty);
      set => SetValue(SelectedItemsProperty, value);
    }

    // Propagate selected items from model to view
    private void SelectItems()
    {
      _viewHandled = true;
      AssociatedObject.SelectedItems.Clear();
      if (SelectedItems != null)
      {
        foreach (var item in SelectedItems)
          AssociatedObject.SelectedItems.Add(item);
      }
      _viewHandled = false;
    }

    // Propagate selected items from view to model
    private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
      if (_viewHandled) return;
      if (AssociatedObject.Items.SourceCollection == null) return;

      SelectedItems = new BindableCollection<string>(AssociatedObject.SelectedItems.OfType<string>());
    }

    // Re-select items when the set of items changes
    private void OnListBoxItemsChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
      if (_viewHandled) return;
      if (AssociatedObject.Items.SourceCollection == null) return;

      SelectItems();
    }

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.SelectionChanged += OnListBoxSelectionChanged;
      ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged += OnListBoxItemsChanged;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
      base.OnDetaching();

      if (AssociatedObject != null)
      {
        AssociatedObject.SelectionChanged -= OnListBoxSelectionChanged;
        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= OnListBoxItemsChanged;
      }
    }
  }
}
