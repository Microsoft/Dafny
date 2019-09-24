package dafny;

import java.math.BigInteger;
import java.util.*;
import java.util.function.Function;
import java.util.function.Consumer;
import java.util.stream.Collectors;

public class DafnySequence<T> implements Iterable<T> {
    /*
    Invariant: forall 0<=i<length(). seq[i] == T || null
    Property: DafnySequences are immutable. Any methods that seem to edit the DafnySequence will only return a new
    DafnySequence
    Todo: DafnySequence Invariants and Properties
     */
    protected ArrayList<T> seq;

    public DafnySequence() {
        seq = new ArrayList<>();
    }

    public static DafnySequence<Character> asString(String s){
        return new DafnySequence<>(s.chars()
                .mapToObj(e -> (char)e)
                .collect(Collectors.toList()));
    }

    private DafnySequence(List<T> l, int i, T t){
        seq = new ArrayList<>(l);
        seq.set(i, t);
    }

    public DafnySequence(List<T> l) {
        assert l != null: "Precondition Violation";
        seq = new ArrayList<>(l);
    }

    public DafnySequence(DafnySequence<T> other) {
        assert other != null : "Precondition Violation";
        seq = new ArrayList<>(other.seq);
    }

    public static <T> DafnySequence<T> Create(BigInteger length, Function<BigInteger, T> init) {
        ArrayList<T> values = new ArrayList<>();
        for(BigInteger i = BigInteger.ZERO; i.compareTo(length) < 0; i = i.add(BigInteger.ONE)) {
            values.add(init.apply(i));
        }
        return new DafnySequence<>(values);
    }

    // Determines if this DafnySequence is a prefix of other
    public boolean isPrefixOf(DafnySequence<T> other) {
        assert other != null : "Precondition Violation";
        if (other.length() < length()) return false;
        for (int i = 0; i < length(); i++) {
            if (seq.get(i) != other.select(i)) return false;
        }

        return true;
    }

    // Determines if this DafnySequence is a proper prefix of other
    public boolean isProperPrefixOf(DafnySequence<T> other) {
        assert other != null : "Precondition Violation";
        return length() < other.length() && isPrefixOf(other);
    }

    public DafnySequence<T> concatenate(DafnySequence<T> other) {
        assert other != null : "Precondition Violation";
        List<T> l = new ArrayList<>(seq);
        l.addAll(other.seq);
        return new DafnySequence<>(l);
    }

    public T select(int i) {
        assert i >= 0 : "Precondition Violation";
        return seq.get(i);
    }

    public T select(UByte i) {
        return select(i.intValue());
    }

    public T select(UShort i) {
        return select(i.intValue());
    }

    public T select(UInt i) {
        return select(i.asBigInteger());
    }

    public T select(long i) {
        return select(BigInteger.valueOf(i));
    }

    public T select(ULong i) {
        return select(i.asBigInteger());
    }

    public T select(BigInteger i) {
        return select(i.intValue());
    }

    public int length() {
        return seq.size();
    }

    public DafnySequence<T> update(int i, T t) {
        //todo: should we allow i=length, and return a new sequence with t appended to the sequence?
        assert 0 <= i && i < length(): "Precondition Violation";
        return new DafnySequence<>(seq, i, t);
    }

    public DafnySequence<T> update(BigInteger b, T t) {
        //todo: should we allow i=length, and return a new sequence with t appended to the sequence?
        assert b.compareTo(BigInteger.ZERO) >= 0 &&
               b.compareTo(BigInteger.valueOf(length())) < 0: "Precondition Violation";
        return new DafnySequence<>(seq, b.intValue(), t);
    }

    public boolean contains(T t) {
        assert t != null : "Precondition Violation";
        return seq.contains(t);
    }

    // Returns the subsequence of values [lo..hi)
    public DafnySequence<T> subsequence(int lo, int hi) {
        assert lo >= 0 && hi >= 0 && hi >= lo : "Precondition Violation";
        return new DafnySequence<>(seq.subList(lo, hi));
    }


    // Returns the subsequence of values [lo..length())
    public DafnySequence<T> drop(int lo) {
        assert lo >= 0 && lo < length() : "Precondition Violation";
        return new DafnySequence<>(seq.subList(lo, length()));
    }

    public DafnySequence<T> drop(UByte lo) {
        return drop(lo.intValue());
    }

    public DafnySequence<T> drop(UShort lo) {
        return drop(lo.intValue());
    }

    public DafnySequence<T> drop(UInt lo) {
        return drop(lo.asBigInteger());
    }

    public DafnySequence<T> drop(long lo) {
        return drop(BigInteger.valueOf(lo));
    }

    public DafnySequence<T> drop(ULong lo) {
        return drop(lo.asBigInteger());
    }

    public DafnySequence<T> drop(BigInteger lo) {
        return drop(lo.intValue());
    }


    // Returns the subsequence of values [0..hi)
    public DafnySequence<T> take(int hi) {
        assert hi >= 0 && hi <= length() : "Precondition Violation";
        return new DafnySequence<>(seq.subList(0, hi));
    }

    public DafnySequence<T> take(UByte hi) {
        return take(hi.intValue());
    }

    public DafnySequence<T> take(UShort hi) {
        return take(hi.intValue());
    }

    public DafnySequence<T> take(UInt hi) {
        return take(hi.asBigInteger());
    }

    public DafnySequence<T> take(long hi) {
        return take(BigInteger.valueOf(hi));
    }

    public DafnySequence<T> take(ULong hi) {
        return take(hi.asBigInteger());
    }

    public DafnySequence<T> take(BigInteger hi) {
        return take(hi.intValue());
    }

    public DafnySequence<DafnySequence<T>> slice(List<Integer> l) {
        assert l != null : "Precondition Violation";
        List<DafnySequence<T>> list = new ArrayList<>();
        int curr = 0;
        for (Integer i : l) {
            assert i != null : "Precondition Violation";
            list.add(new DafnySequence<>(subsequence(curr, curr + i)));
            curr += i;
        }

        return new DafnySequence<>(list);
    }

    public DafnyMultiset<T> asDafnyMultiset() {
        return new DafnyMultiset<>(seq);
    }

    @Override
    public Spliterator<T> spliterator() {
        return seq.spliterator();
    }

    @Override
    public Iterator<T> iterator() {
        return seq.iterator();
    }

    @Override
    @SuppressWarnings("UNCHECKED_CAST")
    public boolean equals(Object obj) {
        if (this == obj) return true;
        if (obj == null) return false;
        if (getClass() != obj.getClass()) return false;
        DafnySequence o = (DafnySequence) obj;
        if (length() != o.length()) return false;
        return seq.equals(o.seq);
    }

    @Override
    public int hashCode() {
        return seq.hashCode();
    }

    @Override
    public String toString() {
        return seq.toString();
    }

    @SuppressWarnings("unchecked")
    public String verbatimString(){
        StringBuilder builder = new StringBuilder(seq.size());
        for(Character ch: (ArrayList<Character>) seq)
        {
            builder.append(ch);
        }
        return builder.toString();
    }

    public HashSet<T> UniqueElements() {
        return new HashSet<>(seq);
    }
}
