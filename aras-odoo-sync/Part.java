import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.dataformat.xml.annotation.JacksonXmlProperty;
import com.fasterxml.jackson.dataformat.xml.annotation.JacksonXmlRootElement;

@JacksonXmlRootElement(localName = "part")
@JsonIgnoreProperties(ignoreUnknown = true)
public class Part {
	@JacksonXmlProperty(localName = "item_number") 
    @JsonProperty("default_code")
	private String item_number;
    
	@JacksonXmlProperty(localName = "classification") 
    @JsonIgnore  
	private String classification;
    
	@JacksonXmlProperty(localName = "name") 
    @JsonProperty("name")
	private String name;
   
    @JacksonXmlProperty(localName = "description") 
    @JsonProperty("description_sale")
	private String description;
    
	@JacksonXmlProperty(localName = "makebuy") 
    @JsonIgnore  
	private String makebuy;
    
	@JacksonXmlProperty(localName = "image") 
    @JsonIgnore  
	private String image;
	
	public Part() {
		
	}
	
	public Part(String item_number, String classification, String name, String description, String makebuy,
			String image) {
		super();
		this.item_number = item_number;
		this.classification = classification;
		this.name = name;
		this.description = description;
		this.makebuy = makebuy;
		this.image = image;
	}

	public String getItem_number() {
		return item_number;
	}

	public void setItem_number(String item_number) {
		this.item_number = item_number;
	}

	public String getClassification() {
		return classification;
	}

	public void setClassification(String classification) {
		this.classification = classification;
	}

	public String getName() {
		return name;
	}

	public void setName(String name) {
		this.name = name;
	}

	public String getDescription() {
		return description;
	}

	public void setDescription(String description) {
		this.description = description;
	}

	public String getMakebuy() {
		return makebuy;
	}

	public void setMakebuy(String makebuy) {
		this.makebuy = makebuy;
	}

	public String getImage() {
		return image;
	}

	public void setImage(String image) {
		this.image = image;
	}

	@Override
	public String toString() {
		return "Part [item_number=" + item_number + ", classification=" + classification + ", name=" + name
				+ ", description=" + description + ", makebuy=" + makebuy + ", image=" + image + "]";
	}
	
}
