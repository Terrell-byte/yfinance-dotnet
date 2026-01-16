namespace YFinance.Yahoo.Entities;

<summary>
    IndustryTag is a class that represents Yahoo Finance's internal industry tag.

    List of tags:
    # Technology
    - ^YH31130020-latest-news: Semiconductors
    - ^YH31110030-latest-news: Software Infrastructure
    - ^YH31120030-latest-news: Consumer Electronics
    - ^YH31110020-latest-news: Software Application
    - ^YH31130010-latest-news: Semiconductor Equipment & Materials
    - ^YH31110010-latest-news: Information Technology Services
    - ^YH31120010-latest-news: Communications Equipment
    - ^YH31120020-latest-news: Computer Hardware
    - ^YH31120040-latest-news: Electronic Components
    - ^YH31120060-latest-news: Scientific & Technical Instruments
    - ^YH31130030-latest-news: Solar
    - ^YH31120050-latest-news: Electronics & Computer Distribution

    # Financial Services
    
</summary>
public record IndustryTag(string sector, string industry, string listName);

