using System;
using System.Collections.Generic;
using static PokerDeck;

class PokerDeck
{
    // 카드 모양 (Suit)과 카드 값 (Rank)을 표현하는 클래스
    public class Card
    {
        public string Suit { get; set; }  // 다이아, 클로버, 하트, 스페이드
        public string Rank { get; set; }  // A~10, J, Q, K

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }

    // 덱을 저장할 리스트
    private List<Card> deck;

    public PokerDeck()
    {
        InitializeDeck();  // 덱 초기화
    }

    // 덱을 초기화하는 함수
    private void InitializeDeck()
    {
        string[] suits = { "스쿼트", "스쿼트", "오른발 런지", "왼발 런지" };
        string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        deck = new List<Card>();

        // 52장의 카드 생성
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                deck.Add(new Card { Suit = suit, Rank = rank });
            }
        }

        // 조커 카드 추가 (조커는 보통 두 장으로 간주함)
        deck.Add(new Card { Suit = "Joker", Rank = "Joker" });
        deck.Add(new Card { Suit = "Joker", Rank = "Joker" });
    }

    // 카드를 한 장 뽑는 함수
    public Card DrawCard()
    {
        if (deck.Count == 0)
        {
            throw new InvalidOperationException("No cards left in the deck!");
        }

        // 랜덤하게 한 장 뽑기
        Random rand = new Random();
        int index = rand.Next(deck.Count);
        Card drawnCard = deck[index];

        // 뽑은 카드를 덱에서 제거
        deck.RemoveAt(index);

        return drawnCard;
    }

    // 남은 카드 수 확인
    public int RemainingCards()
    {
        return deck.Count;
    }
}

class Program
{
    static void Main(string[] args)
    {
        PokerDeck deck = new PokerDeck();
        Console.WriteLine("Press 'o' to draw a card, or any other key to exit.");

        while (deck.RemainingCards() > 0)
        {
            var input = Console.ReadKey(true).KeyChar;
            if (input == 'o')
            {
                Card drawnCard = deck.DrawCard();
                Console.WriteLine($"You drew: {drawnCard}");
                Console.WriteLine($"Cards remaining: {deck.RemainingCards()}");
            }
            else
            {
                if (deck.RemainingCards() == 0)
                {
                    break;
                }
            }
        }

        Console.WriteLine("No more cards to draw!");
    }
}
