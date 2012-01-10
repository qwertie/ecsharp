using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniTestRunner
{
/**********************************************************************
 * 
 * Update Controls .NET
 * Copyright 2010 Michael L Perry
 * MIT License
 * 
 * http://updatecontrols.net
 * http://www.codeplex.com/updatecontrols/
 * 
 **********************************************************************/
	/// <summary>
	/// A collection that maps new objects to old, equivalent versions of the
	/// same objects. It is typically used with LINQ during a Dependent update.
	/// </summary>
	/// <typeparam name="T">Type of object to recycle. When using the MVVM design
	/// pattern, T is typically a type of view-model that wraps around a model
	/// type.</typeparam>
	/// <remarks>
	/// This class helps implement the MVVM pattern with UpdateControls. In this
	/// pattern, you typically write a "Model" class which contains all the state
	/// information for your models, and a "ViewModel" class which is a thin 
	/// wrapper around the Model. The ViewModel should be stateless, except for
	/// temporary information that is only meaningful in the GUI, such as an 
	/// "IsSelected" flag that represents whether the ViewModel is selected in
	/// a ListBox.
	/// <para/>
	/// In the UpdateControls paradigm, you will typically create (at most) one
	/// ViewModel object for each Model, and some kind of dependent collection is 
	/// used to keep the set of ViewModels synchronized with the set of Models. 
	/// RecycleBin plays an important role in this paradigm. If you use a class 
	/// such as <see cref="DependentList{T}"/>, it will use a RecycleBin for you,
	/// but if you use <see cref="Dependent"/> directly then you may need to 
	/// create a RecycleBin yourself.
	/// <para/>
	/// RecycleBin has two purposes: (1) it disposes old objects that are no 
	/// longer in use, if T implements IDisposable; and (2) it preserves any state 
	/// information in the ViewModel wrappers.
	/// <para/>
	/// Typical usage is as follows: you first construct a RecycleBin within a 
	/// <see cref="Dependent"/>'s update function (assuming that the Dependent 
	/// controls a collection.) You fill the recycle bin with the old contents 
	/// of your collection of ViewModels, then construct a new collection of 
	/// ViewModels (from scratch, e.g. using a LINQ query over your models), and 
	/// pass each new ViewModel through the <see cref="Extract(T)"/> method. If 
	/// the new ViewModel represents a Model that was in the old collection, 
	/// Extract returns the old ViewModel; otherwise it returns the new ViewModel.
	/// This ensures that the ViewModel state is preserved. For example, if your 
	/// ViewModel has an IsSelected flag, then failing to use a RecycleBin would 
	/// cause any selected objects to become deselected whenever the Dependent 
	/// is updated (assuming IsSelected is false by default).
	/// <para/>
	/// The recycle bin extracts objects based on a prototype. If
	/// the recycle bin contains an object matching the prototype
	/// according to <see cref="Object.GetHashCode"/> and
	/// <see cref="Object.Equals(object)"/>, then that matching object
	/// is extracted. If not, the prototype itself is used. It is
	/// imperitive that you properly implement GetHashCode and
	/// Equals in your recycled classes.
	/// <para/>
	/// If T is a ViewModel class, then it generally suffices for T.GetHashCode 
	/// to call GetHashCode on the wrapped Model, and for T.Equals to compare
	/// the two wrapped objects for equality.
	/// <para/>
	/// In general, your implementation of GetHashCode and Equals must only 
	/// consider fields that do not change. If a field can be changed, or is
	/// itself dependent, then it must not be used either as part of the
	/// hash code, or to determine equality. The best practice is to
	/// implement GetHashCode and Equals in terms of fields that are
	/// initialized by the constructor, and are thereafter immutable.
	/// <para/>
	/// The advantage of RecycleBin is not found in any time or memory savings.
	/// In fact, using RecycleBin in most cases adds a small amount of overhead.
	/// However, the advantage comes from preserving the dynamic and
	/// dependent state of the recycled objects. If your depenent collection
	/// contains only immutable objects (such as strings), there is no
	/// advantage to using a RecycleBin.
	/// </remarks>
	public class RecycleBin<T> : IDisposable
	{
		private Dictionary<T, T> _objects = new Dictionary<T, T>();

		// This list is created only if there are duplicates AND T is IDisposable.
		// Its sole purpose is to allow us to dispose duplicates, since _objects
		// cannot hold duplicates.
		private List<T> _duplicates;

		static readonly bool IsTDisposable = typeof(IDisposable).IsAssignableFrom(typeof(T));

		/// <summary>
		/// Creates an empty recycle bin.
		/// </summary>
		/// <remarks>
		/// The recycle bin should be filled with objects from a dependent
		/// collection, and the collection should be emptied. Then it can be
		/// repopulated by extraction from the bin.
		/// </remarks>
		public RecycleBin()
		{
		}
		/// <summary>
		/// Creates an recycle bin containing the specified objects.
		/// </summary>
		public RecycleBin(IEnumerable<T> objects)
		{
			AddRange(objects);
		}

		/// <summary>
		/// Add an object to the recycle bin.
		/// </summary>
		/// <param name="recyclableObject">The object to put in the recycle bin.</param>
		public void AddObject(T recyclableObject)
		{
			if (recyclableObject != null)
			{
				if (IsTDisposable && _objects.ContainsKey(recyclableObject)) {
					if (_duplicates == null)
						_duplicates = new List<T>();
					_duplicates.Add(recyclableObject);
				} else
					_objects[recyclableObject] = recyclableObject;
			}
		}

		public void AddRange(IEnumerable<T> objects)
		{
			if (objects != null)
				foreach (var obj in objects)
					AddObject(obj);
		}

		/// <summary>
		/// If a matching object is in the recycle bin, remove and return it.
		/// Otherwise, return the prototype.
		/// </summary>
		/// <param name="prototype">An object equal to the one to be extracted.</param>
		/// <returns>The matching object that was added to the recycle bin, or
		/// the prototype if no such object is found.</returns>
		public T Extract(T prototype)
		{
			// See if a matching object is already in the recycle bin.
			T match;
			if (_objects.TryGetValue(prototype, out match))
			{
				if (IsTDisposable)
					((IDisposable)prototype).Dispose();
				_objects.Remove(prototype);
				return match;
			}
			// No, so use the prototype.
			return prototype;
		}

		public IEnumerable<T> Recycle(IEnumerable<T> newList)
		{
			foreach (T item in newList)
				yield return Extract(item);
		}

		/// <summary>
		/// Disposes all objects remaining in the recycle bin.
		/// </summary>
		/// <remarks>
		/// Call this method at the end of the update function. Any objects
		/// that have not been recycled will be disposed, thus removing any
		/// dependencies they may have. This allows cached objects to be
		/// unloaded and garbage collected.
		/// </remarks>
		public void Dispose()
		{
			if (IsTDisposable)
			{
				foreach (T obj in _objects.Values)
					((IDisposable)obj).Dispose();
				if (_duplicates != null)
					foreach (T obj in _duplicates)
						((IDisposable)obj).Dispose();
			}
        }
    }
}
