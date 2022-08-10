from copy import copy
from operator import contains


class Node :
    
    def __init__(self) -> None:
        self.neibours: dict[Node, float]
        self.parent: Node = None 
        self.pathCost: float = 0.0
        self.visitedNodes: set[Node] = set([self])

    def __eq__(self, __o: object) -> bool:
        raise NotImplementedError()

    def __hash__(self) -> int:
        raise NotImplementedError()
    
    def h(self) -> float :
        raise NotImplementedError()

    def addVisited(self, newNode) :
        self.visitedNodes.add(newNode)

    def hasVisited(self, node) :
        return node in self.visitedNodes
    
    def traceBack(self) :
        result: list[Node] = []
        currentNode = self 
        while currentNode != None :
            result.insert(0, currentNode)
            currentNode = currentNode.parent
        return result

    def getNotVisitedNeibours(self) :
        result: list[Node] = []
        for node in self.neibours :
            if (self.hasVisited(node)): continue
            node_copy = copy(node)
            node_copy.visitedNodes = set(self.visitedNodes)
            node_copy.addVisited(node)
            node_copy.parent = self 
            node_copy.pathCost += self.neibours[node]
            result.append(node_copy)
        return result

    def getNeibours(self) :
        result: list[Node] = []
        for node in self.neibours :
            node_copy = copy(node)
            node_copy.parent = self 
            node_copy.pathCost += self.neibours[node]
            result.append(node_copy)
        return result
            



class PriorityQueue :

    def __init__(self) -> None:
        self._list: list[Node] = []

    def __getitem__(self, index: int) -> Node :
        return self._list[index]

    def isEmpty(self) -> bool :
        return len(self._list) == 0
    
    def append(self, element: Node) :
        if self.isEmpty() : 
            self._list.append(element)
            return
        for i in range(len(self._list)) :
            if (element.h() < self[i].h()) :
                self._list.insert(i, element)
                return 
        self._list.append(element)

    def appendAll(self, elements: list[Node]) :
        if elements == None : return 
        for e in elements :
            self.append(e)
    
    def popFirst(self) -> Node :
        return self._list.pop(0)
    
    def popLast(self) -> Node :
        return self._list.pop()



def aStarSearch(start: Node, dest: Node) -> list[Node] :
    
    queue = PriorityQueue()
    queue.append(start)

    while True :

        if queue.isEmpty() :
            return None 

        currentNode = queue.popFirst()

        if currentNode == dest :
            return currentNode.traceBack()
        
        queue.appendAll(currentNode.getNotVisitedNeibours())