// Dafny should emit exit value 1
// RUN: %dafny /compile:0 /z3exe:Output/binz/z3 "%s" > "%t" || echo ERROR EXIT >> "%t"
// RUN: %diff "%s.expect" "%t"

method m() {
  assert 1 + 1 == 2;
}
