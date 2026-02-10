using System;
using System.Collections.Generic;

public interface TreeMapComparator<K>
{
    int Compare(K first, K second);
}

public class MyTreeMap<K, V>
{
    private readonly TreeMapComparator<K> comparator;
    private Node root;
    private int size;

    private class Node
    {
        public K Key;
        public V Value;
        public Node Left;
        public Node Right;

        public Node(K key, V value)
        {
            Key = key;
            Value = value;
            Left = null;
            Right = null;
        }
    }

    private class DefaultComparator : TreeMapComparator<K>
    {
        public int Compare(K first, K second)
        {
            return Comparer<K>.Default.Compare(first, second);
        }
    }

    // 1) MyTreeMap() — естественный порядок
    public MyTreeMap()
    {
        comparator = new DefaultComparator();
        root = null;
        size = 0;

        // Проверим что тип K вообще сравним, иначе Comparer<K>.Default может упасть в неожиданный момент
        TryCompareDefault();
    }

    // 2) MyTreeMap(TreeMapComparator comp) — по указанному компаратору
    public MyTreeMap(TreeMapComparator<K> comp)
    {
        if (comp == null)
        {
            throw new ArgumentNullException(nameof(comp), "Компаратор не может быть null.");
        }

        comparator = comp;
        root = null;
        size = 0;
    }

    // 3) clear()
    public void Clear()
    {
        root = null;
        size = 0;
    }

    // 8) isEmpty()
    public bool IsEmpty()
    {
        return size == 0;
    }

    // 12) size()
    public int Size()
    {
        return size;
    }

    // 4) containsKey(object key)
    public bool ContainsKey(object key)
    {
        if (!TryCastKey(key, out K castedKey))
        {
            return false;
        }

        return FindNode(castedKey) != null;
    }

    // 5) containsValue(object value)
    public bool ContainsValue(object value)
    {
        V castedValue;
        if (value == null)
        {
            castedValue = default(V);
        }
        else
        {
            if (!(value is V))
            {
                return false;
            }
            castedValue = (V)value;
        }

        return ContainsValueInSubtree(root, castedValue);
    }

    // 7) get(object key)
    public V Get(object key)
    {
        if (!TryCastKey(key, out K castedKey))
        {
            return default(V);
        }

        Node node = FindNode(castedKey);
        if (node == null)
        {
            return default(V);
        }

        return node.Value;
    }

    // 10) put(K key, V value)
    // Возвращает предыдущее значение, если ключ уже был, иначе default(V)
    public V Put(K key, V value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "Ключ не может быть null.");
        }

        if (root == null)
        {
            root = new Node(key, value);
            size = 1;
            return default(V);
        }

        Node current = root;
        Node parent = null;

        while (current != null)
        {
            parent = current;

            int compareResult = CompareKeys(key, current.Key);
            if (compareResult == 0)
            {
                V oldValue = current.Value;
                current.Value = value;
                return oldValue;
            }
            else if (compareResult < 0)
            {
                current = current.Left;
            }
            else
            {
                current = current.Right;
            }
        }

        int parentCompare = CompareKeys(key, parent.Key);
        if (parentCompare < 0)
        {
            parent.Left = new Node(key, value);
        }
        else
        {
            parent.Right = new Node(key, value);
        }

        size++;
        return default(V);
    }

    // 11) remove(object key)
    // Возвращает удалённое значение, либо default(V), если ключ не найден
    public V Remove(object key)
    {
        if (!TryCastKey(key, out K castedKey))
        {
            return default(V);
        }

        bool wasRemoved;
        V removedValue;
        root = RemoveNode(root, castedKey, out wasRemoved, out removedValue);

        if (wasRemoved)
        {
            size--;
        }

        return removedValue;
    }

    // 6) entrySet()
    // Возвращаем список пар в порядке возрастания ключей
    public List<KeyValuePair<K, V>> EntrySet()
    {
        List<KeyValuePair<K, V>> entries = new List<KeyValuePair<K, V>>();
        AddEntriesInOrder(root, entries);
        return entries;
    }

    // 9) keySet()
    public List<K> KeySet()
    {
        List<K> keys = new List<K>();
        AddKeysInOrder(root, keys);
        return keys;
    }

    // 13) firstKey()
    public K FirstKey()
    {
        if (root == null)
        {
            throw new InvalidOperationException("Отображение пустое: firstKey() недоступен.");
        }

        Node node = GetMinNode(root);
        return node.Key;
    }

    // 14) lastKey()
    public K LastKey()
    {
        if (root == null)
        {
            throw new InvalidOperationException("Отображение пустое: lastKey() недоступен.");
        }

        Node node = GetMaxNode(root);
        return node.Key;
    }

    // 28) firstEntry() — без удаления
    public KeyValuePair<K, V>? FirstEntry()
    {
        if (root == null)
        {
            return null;
        }

        Node node = GetMinNode(root);
        return new KeyValuePair<K, V>(node.Key, node.Value);
    }

    // 29) lastEntry() — без удаления
    public KeyValuePair<K, V>? LastEntry()
    {
        if (root == null)
        {
            return null;
        }

        Node node = GetMaxNode(root);
        return new KeyValuePair<K, V>(node.Key, node.Value);
    }

    // 26) pollFirstEntry() — удаление + возврат
    public KeyValuePair<K, V>? PollFirstEntry()
    {
        if (root == null)
        {
            return null;
        }

        Node node = GetMinNode(root);
        KeyValuePair<K, V> result = new KeyValuePair<K, V>(node.Key, node.Value);

        Remove(node.Key);
        return result;
    }

    // 27) pollLastEntry() — удаление + возврат
    public KeyValuePair<K, V>? PollLastEntry()
    {
        if (root == null)
        {
            return null;
        }

        Node node = GetMaxNode(root);
        KeyValuePair<K, V> result = new KeyValuePair<K, V>(node.Key, node.Value);

        Remove(node.Key);
        return result;
    }

    // 15) headMap(K end): ключи < end
    public MyTreeMap<K, V> HeadMap(K end)
    {
        if (end == null)
        {
            throw new ArgumentNullException(nameof(end), "end не может быть null.");
        }

        MyTreeMap<K, V> result = new MyTreeMap<K, V>(comparator);
        AddRange(root, result, hasStart: false, start: default(K), hasEnd: true, end: end);
        return result;
    }

    // 16) subMap(K start, K end): start <= key < end
    public MyTreeMap<K, V> SubMap(K start, K end)
    {
        if (start == null)
        {
            throw new ArgumentNullException(nameof(start), "start не может быть null.");
        }
        if (end == null)
        {
            throw new ArgumentNullException(nameof(end), "end не может быть null.");
        }

        if (CompareKeys(start, end) > 0)
        {
            throw new ArgumentException("start не может быть больше end.");
        }

        MyTreeMap<K, V> result = new MyTreeMap<K, V>(comparator);
        AddRange(root, result, hasStart: true, start: start, hasEnd: true, end: end);
        return result;
    }

    // 17) tailMap(K start): ключи > start (СТРОГО больше)
    public MyTreeMap<K, V> TailMap(K start)
    {
        if (start == null)
        {
            throw new ArgumentNullException(nameof(start), "start не может быть null.");
        }

        MyTreeMap<K, V> result = new MyTreeMap<K, V>(comparator);
        AddRangeTailStrict(root, result, start);
        return result;
    }

    // 18) lowerEntry(K key): max entry с ключом < key
    public KeyValuePair<K, V>? LowerEntry(K key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "key не может быть null.");
        }

        Node candidate = null;
        Node current = root;

        while (current != null)
        {
            int compareResult = CompareKeys(key, current.Key);
            if (compareResult <= 0)
            {
                current = current.Left;
            }
            else
            {
                candidate = current;
                current = current.Right;
            }
        }

        if (candidate == null)
        {
            return null;
        }

        return new KeyValuePair<K, V>(candidate.Key, candidate.Value);
    }

    // 19) floorEntry(K key): max entry с ключом <= key
    public KeyValuePair<K, V>? FloorEntry(K key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "key не может быть null.");
        }

        Node candidate = null;
        Node current = root;

        while (current != null)
        {
            int compareResult = CompareKeys(key, current.Key);
            if (compareResult < 0)
            {
                current = current.Left;
            }
            else if (compareResult > 0)
            {
                candidate = current;
                current = current.Right;
            }
            else
            {
                return new KeyValuePair<K, V>(current.Key, current.Value);
            }
        }

        if (candidate == null)
        {
            return null;
        }

        return new KeyValuePair<K, V>(candidate.Key, candidate.Value);
    }

    // 20) higherEntry(K key): min entry с ключом > key
    public KeyValuePair<K, V>? HigherEntry(K key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "key не может быть null.");
        }

        Node candidate = null;
        Node current = root;

        while (current != null)
        {
            int compareResult = CompareKeys(key, current.Key);
            if (compareResult < 0)
            {
                candidate = current;
                current = current.Left;
            }
            else
            {
                current = current.Right;
            }
        }

        if (candidate == null)
        {
            return null;
        }

        return new KeyValuePair<K, V>(candidate.Key, candidate.Value);
    }

    // 21) ceilingEntry(K key): min entry с ключом >= key
    public KeyValuePair<K, V>? CeilingEntry(K key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "key не может быть null.");
        }

        Node candidate = null;
        Node current = root;

        while (current != null)
        {
            int compareResult = CompareKeys(key, current.Key);
            if (compareResult < 0)
            {
                candidate = current;
                current = current.Left;
            }
            else if (compareResult > 0)
            {
                current = current.Right;
            }
            else
            {
                return new KeyValuePair<K, V>(current.Key, current.Value);
            }
        }

        if (candidate == null)
        {
            return null;
        }

        return new KeyValuePair<K, V>(candidate.Key, candidate.Value);
    }

    // 22) lowerKey(K key)
    public K LowerKey(K key)
    {
        KeyValuePair<K, V>? entry = LowerEntry(key);
        if (entry == null)
        {
            return default(K);
        }
        return entry.Value.Key;
    }

    // 23) floorKey(K key)
    public K FloorKey(K key)
    {
        KeyValuePair<K, V>? entry = FloorEntry(key);
        if (entry == null)
        {
            return default(K);
        }
        return entry.Value.Key;
    }

    // 24) higherKey(K key)
    public K HigherKey(K key)
    {
        KeyValuePair<K, V>? entry = HigherEntry(key);
        if (entry == null)
        {
            return default(K);
        }
        return entry.Value.Key;
    }

    // 25) ceilingKey(K key)
    public K CeilingKey(K key)
    {
        KeyValuePair<K, V>? entry = CeilingEntry(key);
        if (entry == null)
        {
            return default(K);
        }
        return entry.Value.Key;
    }

    // вспомогателные методы

    private int CompareKeys(K firstKey, K secondKey)
    {
        try
        {
            return comparator.Compare(firstKey, secondKey);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Ошибка сравнения ключей. Проверьте тип K или компаратор.", exception);
        }
    }

    private void TryCompareDefault()
    {
        try
        {
            // просто пробуем сравнить default с default, если тип несравнимый — обычно здесь уже будет исключение
            Comparer<K>.Default.Compare(default(K), default(K));
        }
        catch
        {
            // Иногда default-default сравнение может не упасть, но реальное сравнение упадёт
            // Тогда пользователь увидит исключение при Put/поиске
        }
    }

    private bool TryCastKey(object key, out K castedKey)
    {
        castedKey = default(K);

        if (key == null)
        {
            return false;
        }

        if (!(key is K))
        {
            return false;
        }

        castedKey = (K)key;
        return true;
    }

    private Node FindNode(K key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "Ключ не может быть null.");
        }

        Node current = root;
        while (current != null)
        {
            int compareResult = CompareKeys(key, current.Key);
            if (compareResult == 0)
            {
                return current;
            }
            else if (compareResult < 0)
            {
                current = current.Left;
            }
            else
            {
                current = current.Right;
            }
        }

        return null;
    }

    private bool ContainsValueInSubtree(Node node, V value)
    {
        if (node == null)
        {
            return false;
        }

        if (EqualityComparer<V>.Default.Equals(node.Value, value))
        {
            return true;
        }

        if (ContainsValueInSubtree(node.Left, value))
        {
            return true;
        }

        return ContainsValueInSubtree(node.Right, value);
    }

    private void AddEntriesInOrder(Node node, List<KeyValuePair<K, V>> entries)
    {
        if (node == null)
        {
            return;
        }

        AddEntriesInOrder(node.Left, entries);
        entries.Add(new KeyValuePair<K, V>(node.Key, node.Value));
        AddEntriesInOrder(node.Right, entries);
    }

    private void AddKeysInOrder(Node node, List<K> keys)
    {
        if (node == null)
        {
            return;
        }

        AddKeysInOrder(node.Left, keys);
        keys.Add(node.Key);
        AddKeysInOrder(node.Right, keys);
    }

    private Node GetMinNode(Node node)
    {
        Node current = node;
        while (current.Left != null)
        {
            current = current.Left;
        }
        return current;
    }

    private Node GetMaxNode(Node node)
    {
        Node current = node;
        while (current.Right != null)
        {
            current = current.Right;
        }
        return current;
    }

    private Node RemoveNode(Node node, K key, out bool wasRemoved, out V removedValue)
    {
        wasRemoved = false;
        removedValue = default(V);

        if (node == null)
        {
            return null;
        }

        int compareResult = CompareKeys(key, node.Key);

        if (compareResult < 0)
        {
            node.Left = RemoveNode(node.Left, key, out wasRemoved, out removedValue);
            return node;
        }

        if (compareResult > 0)
        {
            node.Right = RemoveNode(node.Right, key, out wasRemoved, out removedValue);
            return node;
        }

        // Найден узел для удаления
        wasRemoved = true;
        removedValue = node.Value;

        // 1) нет левого ребёнка
        if (node.Left == null)
        {
            return node.Right;
        }

        // 2) нет правого ребёнка
        if (node.Right == null)
        {
            return node.Left;
        }

        // 3) два ребёнка: заменим текущий узел минимальным справа (in-order successor)
        Node successor = GetMinNode(node.Right);

        // сохраним successor данные
        K successorKey = successor.Key;
        V successorValue = successor.Value;

        // удалим successor из правого поддерева
        bool dummyRemoved;
        V dummyValue;
        node.Right = RemoveNode(node.Right, successorKey, out dummyRemoved, out dummyValue);

        // перезапишем текущий узел
        node.Key = successorKey;
        node.Value = successorValue;

        return node;
    }

    private void AddRange(Node node, MyTreeMap<K, V> target, bool hasStart, K start, bool hasEnd, K end)
    {
        if (node == null)
        {
            return;
        }

        AddRange(node.Left, target, hasStart, start, hasEnd, end);

        bool ok = true;

        if (hasStart)
        {
            if (CompareKeys(node.Key, start) < 0)
            {
                ok = false;
            }
        }

        if (hasEnd)
        {
            if (CompareKeys(node.Key, end) >= 0)
            {
                ok = false;
            }
        }

        if (ok)
        {
            target.Put(node.Key, node.Value);
        }

        AddRange(node.Right, target, hasStart, start, hasEnd, end);
    }

    // tailMap по условию: ключи строго больше start
    private void AddRangeTailStrict(Node node, MyTreeMap<K, V> target, K start)
    {
        if (node == null)
        {
            return;
        }

        AddRangeTailStrict(node.Left, target, start);

        if (CompareKeys(node.Key, start) > 0)
        {
            target.Put(node.Key, node.Value);
        }

        AddRangeTailStrict(node.Right, target, start);
    }
}

class LabTask18
{
    static void Main()
    {
        MyTreeMap<int, string> map = new MyTreeMap<int, string>();

        Console.WriteLine("Добавляем элементы");
        map.Put(5, "five");
        map.Put(2, "two");
        map.Put(8, "eight");
        map.Put(1, "one");
        map.Put(3, "three");

        Console.WriteLine("Размер: " + map.Size());
        Console.WriteLine();

        Console.WriteLine("Все элементы (entrySet):");
        foreach (var entry in map.EntrySet())
        {
            Console.WriteLine(entry.Key + " -> " + entry.Value);
        }
        Console.WriteLine();

        Console.WriteLine("firstKey: " + map.FirstKey());
        Console.WriteLine("lastKey: " + map.LastKey());
        Console.WriteLine();

        Console.WriteLine("get(3): " + map.Get(3));
        Console.WriteLine("containsKey(4): " + map.ContainsKey(4));
        Console.WriteLine("containsValue(\"two\"): " + map.ContainsValue("two"));
        Console.WriteLine();

        Console.WriteLine("lowerKey(5): " + map.LowerKey(5));
        Console.WriteLine("floorKey(5): " + map.FloorKey(5));
        Console.WriteLine("higherKey(5): " + map.HigherKey(5));
        Console.WriteLine("ceilingKey(5): " + map.CeilingKey(5));
        Console.WriteLine();

        Console.WriteLine("pollFirstEntry:");
        var first = map.PollFirstEntry();
        if (first != null)
        {
            Console.WriteLine(first.Value.Key + " -> " + first.Value.Value);
        }

        Console.WriteLine("pollLastEntry:");
        var last = map.PollLastEntry();
        if (last != null)
        {
            Console.WriteLine(last.Value.Key + " -> " + last.Value.Value);
        }

        Console.WriteLine();
        Console.WriteLine("Элементы после удаления:");
        foreach (var entry in map.EntrySet())
        {
            Console.WriteLine(entry.Key + " -> " + entry.Value);
        }

        Console.WriteLine();
        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}
